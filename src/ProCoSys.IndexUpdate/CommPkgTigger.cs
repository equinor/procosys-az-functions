using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ProCoSys.IndexUpdate.IndexModel;
using ProCoSys.IndexUpdate.Topics;

namespace ProCoSys.IndexUpdate
{
    public class CommPkgTigger
    {
        private IConfiguration _configuration;

        public CommPkgTigger(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [FunctionName("CommPkgTigger")]
        public void Run([ServiceBusTrigger("commpkg", "search_commpkg", Connection = "ConnectionString")]string mySbMsg, ILogger log)
        {
            log.LogInformation($"C# ServiceBus topic trigger function processed message: {mySbMsg}");

            try
            {
                // Search Index Configuration
                //var indexName = Environment.GetEnvironmentVariable("Index_Name");
                //var indexEndpoint = Environment.GetEnvironmentVariable("Index_Endpoint");
                //var indexKey = Environment.GetEnvironmentVariable("Index_Key");

                var indexName = _configuration.GetValue<string>("Index_Name"); 
                var indexEndpoint = _configuration.GetValue<string>("Index_Endpoint");  
                var indexKey = _configuration.GetValue<string>("Index_Key"); 


                if (indexName == null || indexEndpoint == null || indexKey == null)
                {
                    log.LogError($"Invalid configuration");
                    return;
                }

                // Get the service endpoint and API key from the environment
                Uri endpoint = new Uri(indexEndpoint);

                // Create a client
                AzureKeyCredential credential = new AzureKeyCredential(indexKey);
                SearchClient client = new SearchClient(endpoint, indexName, credential);

                // Deserialize message
                var msg = JsonSerializer.Deserialize<CommPkgTopic>(mySbMsg);

                // Calculate key for document
                var keyString = $"commpkg:{msg.Plant}:{msg.ProjectName}:{ msg.CommPkgNo}";
                var keyBytes = Encoding.UTF8.GetBytes(keyString);
                var key = Convert.ToBase64String(keyBytes);

                // Create new document
                var doc = new IndexDocument
                {
                    Key = key,
                    LastUpdated = msg.LastUpdated,
                    Plant = msg.Plant,
                    PlantName = msg.PlantName,
                    Project = msg.ProjectName,
                    ProjectNames = msg.ProjectNames ?? new List<string>(),

                    CommPkg = new CommPkg
                    {
                        CommPkgNo = msg.CommPkgNo,
                        Description = msg.Description,
                        DescriptionOfWork = msg.DescriptionOfWork,
                        Remark = msg.Remark,
                        Responsible = msg.ResponsibleCode + " " + msg.ResponsibleDescription,
                        Area = msg.AreaCode + " " + msg.AreaDescription
                    }
                };

                // Add or update the document in the index index
                IndexDocumentsBatch<IndexDocument> batch = IndexDocumentsBatch.Create(
                    IndexDocumentsAction.MergeOrUpload(doc));
                IndexDocumentsOptions options1 = new IndexDocumentsOptions { ThrowOnAnyError = true };
                var resp = client.IndexDocuments(batch, options1);
                Console.WriteLine(resp.Value);
            }
            catch (Exception e)
            {
                log.LogInformation($"Error processing message: {mySbMsg} \nError: {e.Message}");
            }
        }
    }
}
