
using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Queue;
using WeatherBeer.Models;

namespace WeatherBeer
{
    public static class BeerFunctions
    {
        [FunctionName("BeerTime")]
        public static async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)]HttpRequest req, ILogger log)
        { 
            try
            {
                string cityName = req.Query["city"];
                string countryCode = req.Query["country"];
                countryCode = countryCode.ToUpper();

                // Check if country or city is valid
                if (!string.IsNullOrWhiteSpace(countryCode) && !string.IsNullOrWhiteSpace(cityName))
                {
                    var storageAccount = CloudStorageAccount.Parse(Environment.GetEnvironmentVariable("StorageConnectionString"));

                    string blobContainerReference = "weatherblobs";
                    CloudBlobContainer blobContainer = await CreateBlobContainer(storageAccount, blobContainerReference);

                    string guid = Guid.NewGuid().ToString();
                    string blobUrl = await CreateBlob(guid, log);

                    CloudQueueMessage cloudQueueMessage = CreateApiMessage(cityName, countryCode, blobUrl, guid);
                    CloudQueueClient client = storageAccount.CreateCloudQueueClient();
                    await AddMessageToQueue(cloudQueueMessage, client);

                    string result = String.Format($"Beerreport succesfully generated for {cityName},{countryCode}. Link: {blobUrl}. This report is accessible for 10 minutes.");

                    log.LogInformation(result);
                    return new OkObjectResult(result);
                }
                else
                {
                    return new BadRequestObjectResult("Country or City is invalid");
                }

            }
            catch(Exception e)
            {
                log.LogError(e.Message);
                return new BadRequestObjectResult("An error occured.");
            }
        }

        private static async Task<CloudBlobContainer> CreateBlobContainer(CloudStorageAccount storageAccount, string reference)
        {
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
            CloudBlobContainer blobContainer = blobClient.GetContainerReference(reference);
            await blobContainer.CreateIfNotExistsAsync();

            BlobContainerPermissions permissions = new BlobContainerPermissions
            {
                PublicAccess = BlobContainerPublicAccessType.Off
            };
            await blobContainer.SetPermissionsAsync(permissions);
            return blobContainer;
        }

        private async static Task<string> CreateBlob(string guid, ILogger log)
        {
            var storageAccount = CloudStorageAccount.Parse(Environment.GetEnvironmentVariable("AzureWebJobsStorage"));
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
            CloudBlobContainer blobContainer = blobClient.GetContainerReference("weatherblobs");
            await blobContainer.CreateIfNotExistsAsync();

            var sas = blobContainer.GetSharedAccessSignature(new SharedAccessBlobPolicy()
            {
                Permissions = SharedAccessBlobPermissions.Read,
                SharedAccessExpiryTime = DateTime.UtcNow.AddMinutes(10)
            });

            string fileName = String.Format($"{guid}.png");
            CloudBlockBlob cloudBlockBlob = blobContainer.GetBlockBlobReference(fileName);
            cloudBlockBlob.Properties.ContentType = "image/png";
            string imageUrl = string.Format($"{blobContainer.StorageUri.PrimaryUri.AbsoluteUri}/{fileName}{sas}");
            return imageUrl;
        }

        private static CloudQueueMessage CreateApiMessage(string cityName, string countryCode, string blobUrl,
            string guid)
        {
            TriggerMessage t = new TriggerMessage
            {
                CityName = cityName,
                CountryCode = countryCode,
                Blob = blobUrl,
                Guid = guid
            };
            var messageAsJson = JsonConvert.SerializeObject(t);
            var cloudQueueMessage = new CloudQueueMessage(messageAsJson);
            return cloudQueueMessage;
        }

        private static async Task AddMessageToQueue(CloudQueueMessage cloudQueueMessage, CloudQueueClient client)
        {
            string openWeatherIn = "openweather";
            var cloudQueue = client.GetQueueReference(openWeatherIn);
            await cloudQueue.CreateIfNotExistsAsync();

            await cloudQueue.AddMessageAsync(cloudQueueMessage);
        }
    }
}
