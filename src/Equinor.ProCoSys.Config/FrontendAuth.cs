using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;

namespace Equinor.ProCoSys.Config
{
    public static class FrontendAuth
    {
        [FunctionName("FrontendAuth")]
        public static IActionResult Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "Frontend/Auth")] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("Processing configuration request...");

            var configConnectionString = Environment.GetEnvironmentVariable("FrontendConfig");

            var environment = EnvironmentSelector.GetEnvironment(req.HttpContext.Request.Headers.FirstOrDefault(x => x.Key == "Origin").Value);
            if (string.IsNullOrWhiteSpace(environment))
            {
                return new BadRequestObjectResult("Invalid origin");
            }
            var configRoot = new ConfigurationBuilder().AddAzureAppConfiguration(options =>
            {
                options
                .Connect(configConnectionString)
                .Select("Auth", environment);
            }).Build();

            var configuration = configRoot.AsEnumerable();

            return (configuration.Count()) switch
            {
                0 => new NotFoundResult(),
                1 => new OkObjectResult(configuration.ElementAt(0).Value),
                _ => new ConflictResult(),
            };
        }
    }
}
