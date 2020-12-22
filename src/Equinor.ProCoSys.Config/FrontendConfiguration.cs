using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;

namespace Equinor.ProCoSys.Config
{
    public static class FrontendConfiguration
    {
        [Authorize]
        [FunctionName("FrontendConfiguration")]
        public static IActionResult Run(
            [HttpTrigger(AuthorizationLevel.User, "get", Route = "Frontend/Configuration")] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("Processing configuration request...");
            var authenticationHeader = req.Headers.FirstOrDefault(x => x.Key == "Authorization");
            if (authenticationHeader.Key == null)
            {
                return new BadRequestResult();
            }
            var token = authenticationHeader.Value.ToString().Replace("Bearer ", null);

            var discoveryEndpoint = Environment.GetEnvironmentVariable("DiscoveryEndpoint");
            var audience = Environment.GetEnvironmentVariable("Audience");
            var issuer = Environment.GetEnvironmentVariable("Issuer");
            if (!TryValidate(token, out JwtSecurityToken securityToken, discoveryEndpoint, audience, issuer))
            {
                return new UnauthorizedResult();
            }

            var currentUserEmail = securityToken.Claims.FirstOrDefault(x => x.Type == "upn")?.Value ?? string.Empty;
            var currentUserDomain = string.Empty;
            if (currentUserEmail.Length > 1)
            {
                currentUserDomain = currentUserEmail[(currentUserEmail.IndexOf('@') + 1)..];
            }

            var environment = GetEnvironment(req.HttpContext.Request.Headers.FirstOrDefault(x => x.Key == "Origin").Value);
            if (string.IsNullOrWhiteSpace(environment))
            {
                return new BadRequestObjectResult("Invalid origin");
            }

            var configConnectionString = Environment.GetEnvironmentVariable("FrontendConfig");

            // Read Configuration
            var configuration = new ConfigurationBuilder()
                .AddAzureAppConfiguration(options =>
                {
                    options
                    .Connect(configConnectionString)
                    .Select(KeyFilter.Any, LabelFilter.Null)
                    .Select(KeyFilter.Any, environment);
                }).Build().AsEnumerable();

            var configSet = new ConfigurationSet();

            // Config
            foreach (var item in configuration.Where(x => !x.Key.StartsWith('.')))
            {
                configSet.Configuration.Add(item.Key, item.Value);
            }

            // Feature Flags
            foreach (var item in configuration.Where(x => x.Key.StartsWith('.')))
            {
                var feature = JsonConvert.DeserializeObject<Feature>(item.Value);

                bool enabled = feature.Enabled;
                if (enabled)
                {
                    var clientFilter = feature
                        .Conditions
                        .Client_Filters
                        .Where(x => x.Name == "Microsoft.Targeting")
                        .FirstOrDefault();
                    if (clientFilter != null)
                    {
                        enabled = clientFilter.Parameters.Audience.Users.Contains(currentUserEmail) || clientFilter.Parameters.Audience.Groups.Any(x => x.Name == currentUserDomain);
                    }
                }

                configSet.FeatureFlags.Add(feature.Id, enabled.ToString());
            }

            // Result
            string json = JsonConvert.SerializeObject(configSet, Formatting.Indented);
            return new OkObjectResult(json);
        }

        public static bool TryValidate(string token, out JwtSecurityToken securityToken, string discoveryEndpoint, string audience, string issuer)
        {
            try
            {
                var configManager = new ConfigurationManager<OpenIdConnectConfiguration>(
                    discoveryEndpoint,
                    new OpenIdConnectConfigurationRetriever());

                OpenIdConnectConfiguration config = configManager.GetConfigurationAsync().Result;
                TokenValidationParameters validationParameters = new TokenValidationParameters
                {
                    //decode the JWT to see what these values should be
                    ValidAudience = audience,
                    ValidIssuer = issuer,
                    ValidateAudience = true,
                    ValidateIssuer = true,
                    IssuerSigningKeys = config.SigningKeys,
                    ValidateLifetime = true
                };

                JwtSecurityTokenHandler tokendHandler = new JwtSecurityTokenHandler();

                tokendHandler.ValidateToken(token, validationParameters, out SecurityToken jwt);

                securityToken = jwt as JwtSecurityToken;
                return true;
            }
            catch (Exception)
            {
                securityToken = null;
                return false;
            }
        }

        public static string GetEnvironment(string origin)
        {
            var devOrigins = Environment.GetEnvironmentVariable("DevOrigins").Split(';').ToList();
            var testOrigins = Environment.GetEnvironmentVariable("TestOrigins").Split(';').ToList();
            var prodOrigins = Environment.GetEnvironmentVariable("ProdOrigins").Split(';').ToList();
            if (devOrigins.Contains(origin))
            {
                return "dev";
            }
            else if (testOrigins.Contains(origin))
            {
                return "test";
            }
            else if (prodOrigins.Contains(origin))
            {
                return "prod";
            }
            else
            {
                return string.Empty;
            }
        }
    }
}
