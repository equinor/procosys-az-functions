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
    public class CommPkgTrigger
    {
        private readonly IConfiguration _configuration;

        public CommPkgTrigger(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [FunctionName("CommPkgTrigger")]
        public void Run([ServiceBusTrigger("commpkg", "search_commpkg", Connection = "ConnectionString")]string mySbMsg, ILogger log)
        {
            log.LogInformation($"C# ServiceBus topic trigger function processed message: {mySbMsg}");

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

                log.LogInformation($"C# Using index {indexName} at {indexEndpoint}");

                // Get the service endpoint and API key from the environment
                Uri endpoint = new Uri($"https://{indexEndpoint}.search.windows.net/");

                // Create a client
                var credential = new AzureKeyCredential(indexKey);
                var client = new SearchClient(endpoint, indexName, credential);

                // Deserialize message
                var msg = JsonSerializer.Deserialize<CommPkgTopic>(mySbMsg);

                // Calculate key for document
                if (msg != null)
                {
                    var key = KeyHelper.GenerateKey($"commpkg:{msg.Plant}:{msg.ProjectName}:{ msg.CommPkgNo}");

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

                    var options = new IndexDocumentsOptions { ThrowOnAnyError = true };

                    // Remove old document from index if CommPkg is moved (has ProjectNameOld)
                    if (msg.ProjectNameOld != null)
                    {
                        // Calculate key for the old document
                        var keyOldDoc = KeyHelper.GenerateKey($"commpkg:{msg.Plant}:{msg.ProjectNameOld}:{msg.CommPkgNo}");

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

                        // Update Projectname for any related McPkgs
                        var searchOptions = new SearchOptions
                        {
                            Filter = $"(McPkg/CommPkgNo eq '{oldDoc.CommPkg.CommPkgNo}')"
                        };

                        var response = (SearchResults<IndexDocument>)client.Search<IndexDocument>("", searchOptions);
                        var docs = new List<IndexDocument>();
                        foreach (SearchResult<IndexDocument> result in response.GetResults())
                        {
                            result.Document.Project = msg.ProjectName;
                            result.Document.ProjectNames = msg.ProjectNames;
                            docs.Add((result.Document));
                        }
                        IndexDocumentsBatch<IndexDocument> mcPkGs = IndexDocumentsBatch.MergeOrUpload(docs);
                        client.IndexDocuments(mcPkGs, options);
                    }

                    // Add or update the document in the index index
                    var batch = IndexDocumentsBatch.Create(IndexDocumentsAction.MergeOrUpload(doc));
                    client.IndexDocuments(batch, options);
                }
            }
            catch (Exception e)
            {
                log.LogInformation($"Error processing message: {mySbMsg} \nError: {e.Message}");
            }
        }
    }
}
