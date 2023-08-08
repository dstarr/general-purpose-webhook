using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace SaaSWebhook
{
    public static class Webhook
    {
        [FunctionName("resource")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("Webhook invoked.");

            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            log.LogInformation(requestBody);

            dynamic body = JsonConvert.DeserializeObject(requestBody);

            var itemToStore = new { 
                id = Guid.NewGuid().ToString(), 
                payload = body 
            };

            log.LogInformation("Saving JSON");
            await StorePayload(itemToStore);
            log.LogInformation("Saved JSON");

            return new OkResult();
        }

        private static async Task StorePayload(dynamic item)
        {
            var databaseName = "WebhookPayloads";
            var containerName = "Requests";

            var cosmosClient = GetCosmosClient();

            var database = await cosmosClient.CreateDatabaseIfNotExistsAsync(databaseName);
            var container = await database.Database.CreateContainerIfNotExistsAsync(containerName, "/id");

            await container.Container.CreateItemAsync(item);
        }

        private static CosmosClient GetCosmosClient()
        {
            var applicationName = "Webhook";
            var primaryKey = Environment.GetEnvironmentVariable("PrimaryKey");
            var endpointUri = Environment.GetEnvironmentVariable("EndpointUri");

            return new CosmosClient(endpointUri, primaryKey, new CosmosClientOptions()
            {
                ApplicationName = applicationName
            });
        }
    }
}
