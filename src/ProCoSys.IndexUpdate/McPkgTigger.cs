using System;
using System.Collections.Generic;
using System.Text.Json;
using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using ProCoSys.IndexUpdate.IndexModel;
using ProCoSys.IndexUpdate.Topics;

namespace ProCoSys.IndexUpdate
{
    public static class McPkgTigger
    {
        [FunctionName("McPkgTigger")]
        public static void Run([ServiceBusTrigger("mcpkg", "search_mcpkg", Connection = "ConnectionString")]string mySbMsg, ILogger log)
        {
            log.LogInformation($"C# ServiceBus topic trigger function processed message: {mySbMsg}");

            try
            {
                // Search Index Configuration
                var indexName = Environment.GetEnvironmentVariable("Index:Name");
                var indexEndpoint = Environment.GetEnvironmentVariable("Index:Endpoint");
                var indexKey = Environment.GetEnvironmentVariable("Index:Key");

                if (indexName == null || indexEndpoint == null || indexKey == null)
                {
                    log.LogError($"Invalid configuration");
                    return;
                }

                // Get the service endpoint and API key from the environment
                var endpoint = new Uri(indexEndpoint);

                // Create a client
                var credential = new AzureKeyCredential(indexKey);
                var client = new SearchClient(endpoint, indexName, credential);

                // Deserialize message
                var msg = JsonSerializer.Deserialize<McPkgTopic>(mySbMsg);

                // Calculate key for document
                var key = KeyHelper.GenerateKey($"mcpkg:{msg.Plant}:{msg.ProjectName}:{msg.CommPkgNo}:{msg.McPkgNo}");

                // Create new document
                var doc = new IndexDocument
                {
                    Key = key,
                    LastUpdated = msg.LastUpdated,
                    Plant = msg.Plant,
                    PlantName = msg.PlantName,
                    Project = msg.ProjectName,
                    ProjectNames = msg.ProjectNames ?? new List<string>(),

                    McPkg = new McPkg
                    {
                        McPkgNo = msg.McPkgNo,
                        CommPkgNo = msg.CommPkgNo,
                        Description = msg.Description,
                        Remark = msg.Remark,
                        Responsible = msg.ResponsibleCode + " " + msg.ResponsibleDescription,
                        Area = msg.AreaCode + " " + msg.AreaDescription,
                        Discipline = msg.Discipline,
                    }
                };

                var options = new IndexDocumentsOptions { ThrowOnAnyError = true };

                // Remove old document from index if McPkg is moved (has CommPkgNoOld or McPkgNoOld)
                if (msg.CommPkgNoOld != null || msg.McPkgNoOld != null)
                {
                    // Calculate key for the old document
                    var keyOldDoc = KeyHelper.GenerateKey($"mcpkg:{msg.Plant}:{msg.ProjectName}:{msg.CommPkgNoOld??msg.CommPkgNo}:{msg.McPkgNoOld??msg.McPkgNo}");

                    //Locate old document in index
                    var oldDoc = (IndexDocument)client.GetDocument<IndexDocument>(keyOldDoc);
                    
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

                // Add or update the document in the index index
                var addBatch = IndexDocumentsBatch.Create(IndexDocumentsAction.MergeOrUpload(doc));
                client.IndexDocuments(addBatch, options);
            }
            catch (Exception e)
            {
                log.LogInformation($"Error processing message: {mySbMsg} \nError: {e.Message}");
            }
        }
    }
}
