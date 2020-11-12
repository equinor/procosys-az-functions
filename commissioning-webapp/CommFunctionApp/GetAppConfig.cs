using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.AddAzureAppConfiguration;

namespace GetAppConfig
{
    public static class GetAppConfig
    {
        private static IConfiguration Configuration { set; get; }

        static GetAppConfig()
        {
            var builder = new ConfigurationBuilder();
            builder.AddAzureAppConfiguration(options =>
            {
                options.Connect(Environment.GetEnvironmentVariable("AppConfigConnectionString")).Select(KeyFilter.Any, LabelFilter.Null).Select(KeyFilter.Any, "dev");
            });
            Configuration = builder.Build();
        }

        [FunctionName("GetAppConfig")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req)
        {
            return new OkObjectResult(Configuration.AsEnumerable());
        }
    }
}