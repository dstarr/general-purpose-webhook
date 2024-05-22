using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Threading.Tasks;

namespace GPWebhook
{
    public static class Webhook
    {
        private static ILogger _log;

        [FunctionName("resource")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req,
            ILogger log)
        {

            _log = log;
            
            _log.LogInformation("Webhook invoked.");
            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic body = JsonConvert.DeserializeObject(requestBody);

            var itemToStore = new { 
                id = Guid.NewGuid().ToString(), 
                payloadBody = body 
            };

            await StorePayload(itemToStore);
            
            return new OkResult();
        }

        private static async Task StorePayload(dynamic item)
        {
            var databaseName = Environment.GetEnvironmentVariable("DatabaseName"); 
            var containerName = Environment.GetEnvironmentVariable("ContainerName");

            _log.LogInformation("Saving JSON");

            var cosmosClient = GetCosmosClient();

            var database = await cosmosClient.CreateDatabaseIfNotExistsAsync(databaseName);
            var container = await database.Database.CreateContainerIfNotExistsAsync(containerName, "/id");

            await container.Container.CreateItemAsync(item);

            _log.LogInformation("Saved JSON");
        }

        private static CosmosClient GetCosmosClient()
        {
            var applicationName = "Webhook";
            var primaryKey = Environment.GetEnvironmentVariable("CosmosPrimaryKey");
            var endpointUri = Environment.GetEnvironmentVariable("CosmosEndpoint");

            return new CosmosClient(endpointUri, primaryKey, new CosmosClientOptions()
            {
                ApplicationName = applicationName
            });
        }
    }
}
