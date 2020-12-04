using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Equinor.ProCoSys.Config
{
    public static class FrontendConfiguration
    {
        [FunctionName("FrontendConfiguration")]
        public static IActionResult Run(
            [HttpTrigger(AuthorizationLevel.User, "get", Route = "Frontend/Configuration")] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("Processing configuration request...");

            ClaimsPrincipal identities = req.HttpContext.User;

            var currentUserEmail = identities.FindFirstValue(ClaimTypes.Email);
            if (string.IsNullOrWhiteSpace(currentUserEmail))
                return new BadRequestResult();
            var currentUserDomain = currentUserEmail[(currentUserEmail.IndexOf('@') + 1)..];

            var bearerToken = req.HttpContext.Request.Headers["Authentication"];
            var configConnectionString = Environment.GetEnvironmentVariable("FrontendConfig");
            var environment = Environment.GetEnvironmentVariable("Environment");

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

            /*
            var features = new List<KeyValuePair<string,object>>();
            foreach (var prop in configuration)
            {
                var json = JsonConvert.DeserializeObject<object>(prop.Value);
                features.Add(new KeyValuePair<string, object>(prop.Key, json));
            }
            return new OkObjectResult(features);
            */
        }
    }
}
