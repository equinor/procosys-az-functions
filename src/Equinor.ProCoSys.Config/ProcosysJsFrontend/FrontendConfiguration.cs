using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Equinor.ProCoSys.Config.ProcosysJsFrontend
{
    public static class FrontendConfiguration
    {
        [Authorize]
        [FunctionName("FrontendConfiguration")]
        public static IActionResult Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "Frontend/Configuration")] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("Processing configuration request...");
            var authenticationHeader = req.Headers.FirstOrDefault(x => x.Key == "Authorization");
            if (authenticationHeader.Key == null)
            {
                return new UnauthorizedResult();
            }
            var token = authenticationHeader.Value.ToString()?.Replace("Bearer ", null) ?? string.Empty;

            var discoveryEndpoint = Environment.GetEnvironmentVariable("DiscoveryEndpoint");
            var audience = Environment.GetEnvironmentVariable("Audience");
            var issuer = Environment.GetEnvironmentVariable("Issuer");
            if (!TokenValidator.TryValidate(token, out JwtSecurityToken securityToken, discoveryEndpoint, audience, issuer))
            {
                return new UnauthorizedResult();
            }

            var currentUserEmail = securityToken.Claims.FirstOrDefault(x => x.Type == "upn")?.Value ?? string.Empty;
            var currentUserDomain = string.Empty;
            if (currentUserEmail.Length > 1)
            {
                currentUserDomain = currentUserEmail[(currentUserEmail.IndexOf('@') + 1)..];
            }

            var originHeader = req.HttpContext.Request.Headers.FirstOrDefault(x => x.Key == "Origin");
            if (originHeader.Key == null)
            {
                return new BadRequestObjectResult("Invalid origin");
            }
            var environment = EnvironmentSelector.GetEnvironment(originHeader.Value);
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
                try
                {
                    object parsedConfig = JsonConvert.DeserializeObject(item.Value);
                    configSet.Configuration.Add(item.Key, parsedConfig);
                }
                catch (Exception e)
                {
                    log.LogWarning(e,$"Failed to format JSON for config key: {item.Key}. If this is a plain value, like a number or string, its expected", item);
                    log.LogInformation(e,$"Falling back to adding RAW value to output config for key: {item.Key}", item);
                    configSet.Configuration.Add(item.Key, item.Value);
                }
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

                configSet.FeatureFlags.Add(feature.Id, enabled);
            }

            // Javascript uses camelCasing
            // lets avoid the headache of forcing them to use PascalCasing inherited from our properties. 
            var contractResolver = new DefaultContractResolver
            {
                NamingStrategy = new CamelCaseNamingStrategy()
            };

            string json = JsonConvert.SerializeObject(configSet, new JsonSerializerSettings
            {
                ContractResolver = contractResolver
            });

            // We need to manually set the content result type, as we have already converted
            // the response to a valid json string. If we try to use jsonResult, then it will reformat the string value. 
            return new ContentResult
            {
                Content = json,
                ContentType = "application/json"
            };
        }
    }
}
