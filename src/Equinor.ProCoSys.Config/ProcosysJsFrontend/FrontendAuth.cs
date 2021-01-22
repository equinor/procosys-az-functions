using System;
using System.Linq;
using System.Web.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using ConflictResult = Microsoft.AspNetCore.Mvc.ConflictResult;

namespace Equinor.ProCoSys.Config.ProcosysJsFrontend
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
                .Select("auth", environment);
            }).Build();

            var configuration = configRoot.AsEnumerable();
            var count = configuration.Count(); // Avoiding multiple enumerations where possible.
            if (count <= 0)
            {
                return new NotFoundResult();
            }

            if (count > 1)
            {
                return new ConflictResult();
            }

            try
            {
                object response = JsonConvert.DeserializeObject(configuration.ElementAt(0).Value);
                return new JsonResult(response);
            }
            catch (Exception e)
            {
                log.LogError(e,"Failed to parse JSON from string in App Configuration", configuration);
                return new InternalServerErrorResult();
            }
        }
    }
}
