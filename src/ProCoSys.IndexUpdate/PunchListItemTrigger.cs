using System;
using System.Collections.Generic;
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
    public class PuncListItemTrigger
    {
        private readonly IConfiguration _configuration;

        public PuncListItemTrigger(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [FunctionName("PuncListItemTrigger")]
        public void Run([ServiceBusTrigger("punchlistitem", "search_punchlistitem", Connection = "ConnectionString")]string mySbMsg, ILogger log)
        {
            log.LogInformation($"C# ServiceBus punchlistitem topic trigger function processed message: {mySbMsg}");

            try
            {
                // Search Index Configuration
                var indexName = _configuration.GetValue<string>("Index_Name");
                var indexEndpoint = _configuration.GetValue<string>("Index_Endpoint");
                var indexKey = _configuration.GetValue<string>("Index_Key");

                if (indexName == null || indexEndpoint == null || indexKey == null)
                {
                    log.LogError($"Invalid configuration");
                    return;
                }

                // Get the service endpoint and API key from the environment
                Uri endpoint = new Uri($"https://{indexEndpoint}.search.windows.net/");

                // Create a client
                var credential = new AzureKeyCredential(indexKey);
                var client = new SearchClient(endpoint, indexName, credential);

                // Deserialize message
                var msg = JsonSerializer.Deserialize<PunchListItemTopic>(mySbMsg);

                // Calculate key for document
                if (msg != null)
                {
                    var key = KeyHelper.GenerateKey($"punchitem:{msg.Plant}:{msg.ProjectName}:{ msg.PunchItemNo}");

                    // Create new document
                    var doc = new IndexDocument
                    {
                        Key = key,
                        LastUpdated = msg.LastUpdated,
                        Plant = msg.Plant,
                        PlantName = msg.PlantName,
                        Project = msg.ProjectName,
                        ProjectNames = msg.ProjectNames ?? new List<string>(),

                        PunchItem = new PunchItem
                        {
                            PunchItemNo = msg.PunchItemNo,
                            Description = msg.Description,
                            TagNo = msg.TagNo,
                            Responsible = msg.ResponsibleCode + " " + msg.ResponsibleDescription,
                            FormType = msg.FormType,
                            Category = msg.Category
                        }
                    };

                    var options = new IndexDocumentsOptions { ThrowOnAnyError = true };


                    // Add or update the document in the index index
                    var addBatch = IndexDocumentsBatch.Create(IndexDocumentsAction.MergeOrUpload(doc));
                    client.IndexDocuments(addBatch, options);
                }
            }
            catch (Exception e)
            {
                log.LogInformation($"Error processing message: {mySbMsg} \nError: {e.Message}");
            }
        }
    }
}
