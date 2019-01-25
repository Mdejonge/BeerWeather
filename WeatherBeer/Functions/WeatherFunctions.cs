using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Newtonsoft.Json;
using WeatherBeer.Models;

namespace WeatherBeer
{
    public static class WeatherFunctions
    {
        [FunctionName("WeatherFunctions")]
        public static async Task RunAsync([QueueTrigger("openweather", Connection = "AzureWebJobsStorage")]string myQueueItem, ILogger log)
        {
            try
            {
                TriggerMessage triggerMessage = (TriggerMessage)JsonConvert.DeserializeObject(myQueueItem, typeof(TriggerMessage));

                RootWeather weather = await WeatherDAO.Instance.GetWeather(triggerMessage.CityName, triggerMessage.CountryCode);

                CloudQueueMessage cloudQueueMessage = CreateCloudQueueMessage(triggerMessage, weather);

                var storageAccount = CloudStorageAccount.Parse(Environment.GetEnvironmentVariable("AzureWebJobsStorage"));
                await PostMessageToQueue(cloudQueueMessage, storageAccount);
            }
            catch(Exception e)
            {
                log.LogError(e.Data.ToString());
            }
        }

        private static CloudQueueMessage CreateCloudQueueMessage(TriggerMessage triggerMessage, RootWeather weather)
        {
            BlobMessage blobMessage = CreateBlobTriggerMessage(triggerMessage, weather);
            var message = JsonConvert.SerializeObject(blobMessage);

            var cloudQueueMessage = new CloudQueueMessage(message);

            return cloudQueueMessage;
        }

        private static BlobMessage CreateBlobTriggerMessage(TriggerMessage triggerMessage, RootWeather weather)
        {
            BlobMessage message = new BlobMessage(weather)
            {
                CityName = triggerMessage.CityName,
                CountryCode = triggerMessage.CountryCode,
                Blob = triggerMessage.Blob,
                Guid = triggerMessage.Guid,
            };
            return message;
        }

        private static async Task PostMessageToQueue(CloudQueueMessage cloudQueueMessage, CloudStorageAccount storageAccount)
        {
            var cloudClient = storageAccount.CreateCloudQueueClient();
            var queue = cloudClient.GetQueueReference("openweather-out");
            await queue.CreateIfNotExistsAsync();

            await queue.AddMessageAsync(cloudQueueMessage);
        }
    }
}
