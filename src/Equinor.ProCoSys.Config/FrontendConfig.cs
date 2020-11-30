using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Equinor.ProCoSys.Config
{
    public static class FrontendConfig
    {
        [FunctionName("Frontend")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("Processing configuration request...");

            var configConnectionString = Environment.GetEnvironmentVariable("ConnectionStrings:FrontendConfig");
            var environment = "dev";
            var builder = new ConfigurationBuilder();
            builder.AddAzureAppConfiguration(options =>
            {
                options
                .Connect(configConnectionString)
                .Select(KeyFilter.Any, LabelFilter.Null)
                .Select(KeyFilter.Any, environment);
            });
            var configuration = builder.Build();

            return new OkObjectResult(configuration.AsEnumerable());
        }
    }
}
