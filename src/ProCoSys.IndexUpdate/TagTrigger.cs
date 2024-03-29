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
    public class TagTrigger
    {
        private readonly IConfiguration _configuration;

        public TagTrigger(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [FunctionName("TagTrigger")]
        public void Run([ServiceBusTrigger("tag", "search_tag", Connection = "ConnectionString")]string mySbMsg, ILogger log)
        {
            log.LogInformation($"C# ServiceBus tag topic trigger function processed message: {mySbMsg}");

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

                var serializerOptions = new JsonSerializerOptions();
                serializerOptions.Converters.Add(new DateTimeConverterUsingDateTimeParse());

                // Deserialize message
                var msg = JsonSerializer.Deserialize<TagTopic>(mySbMsg,serializerOptions);

                // Calculate key for document
                if (msg != null)
                {
                    var key = $"tag_{msg.ProCoSysGuid}";

                    // Create new document
                    var doc = new IndexDocument
                    {
                        Key = key,
                        LastUpdated = msg.LastUpdated,
                        Plant = msg.Plant,
                        PlantName = msg.PlantName,
                        Project = msg.ProjectName,
                        ProCoSysGuid = msg.ProCoSysGuid,
                        ProjectNames = msg.ProjectNames ?? new List<string>(),

                        Tag = new Tag
                        {
                            TagNo = msg.TagNo,
                            McPkgNo = msg.McPkgNo,
                            CommPkgNo = msg.CommPkgNo,
                            Description = msg.Description,
                            Area = msg.AreaCode + " " + msg.AreaDescription,
                            DisciplineCode = msg.DisciplineCode,
                            DisciplineDescription = msg.DisciplineDescription,
                            CallOffNo = msg.CallOffNo,
                            PurchaseOrderNo = msg.PurchaseOrderNo,
                            TagFunctionCode = msg.TagFunctionCode
                        }
                    };

                    var options = new IndexDocumentsOptions { ThrowOnAnyError = true };

                    // Remove old document from index if Tag is moved (has Behavior = delete)
                    if (msg.Behavior == "delete")
                    {
                        //Locate old document in index
                        var oldDoc = (IndexDocument)client.GetDocument<IndexDocument>(key);

                        try
                        {
                            var deleteBatch = IndexDocumentsBatch.Create(IndexDocumentsAction.Delete(oldDoc));
                            client.IndexDocuments(deleteBatch, options);
                        }
                        catch (Exception ex)
                        {
                            throw new Exception($"Failed to delete document: {key}. Message {ex.Message}");
                        }
                    }
                    else
                    {
                        // Add or update the document in the index index
                        var addBatch = IndexDocumentsBatch.Create(IndexDocumentsAction.MergeOrUpload(doc));
                        client.IndexDocuments(addBatch, options);
                    }
                }
            }
            catch (Exception e)
            {
                log.LogInformation($"Error processing message: {mySbMsg} \nError: {e.Message}");
            }
        }
    }
}
