using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;
using WeatherBeer.DAO;
using WeatherBeer.Models;

namespace WeatherBeer
{
    public static class MapsFunction
    {

        [FunctionName("MapsFunction")]
        public static async Task RunAsync([QueueTrigger("openweather-out", Connection = "AzureWebJobsStorage")]string myQueueItem, ILogger log)
        {
            try
            {
                BlobMessage message = (BlobMessage)JsonConvert.DeserializeObject(myQueueItem, typeof(BlobMessage));
                RootWeather weatherRootObject = message.Weather;
                Coord coordinates = weatherRootObject.Coord;

                var storageAccount = CloudStorageAccount.Parse(Environment.GetEnvironmentVariable("AzureWebJobsStorage"));
                CloudBlockBlob cloudBlockBlob = await GetCloudBlockBlob(message, storageAccount);

                HttpResponseMessage httpResponseMessage = await MapsDAO.Instance.GetMaps(coordinates);

                Stream renderedImage = await AddTextToImage(weatherRootObject, httpResponseMessage);

                await cloudBlockBlob.UploadFromStreamAsync(renderedImage);
            }
            catch(Exception e)
            {
                log.LogError(e.Data.ToString());
            }
        }

        private static async Task<Stream> AddTextToImage(RootWeather weatherRootObject, HttpResponseMessage httpResponseMessage)
        {
            int temperature = Convert.ToInt32(weatherRootObject.Main.Temp);
            double windSpeed = weatherRootObject.Wind.Speed;

            Stream responseContent = await httpResponseMessage.Content.ReadAsStreamAsync();

            string text1 = GetBeerMessage(temperature);
            string text2 = string.Format($"Temperature: {temperature}");
            string text3 = string.Format($"Wind: {windSpeed}km/u");

            var renderedImage = ImageHelper.AddTextToImage(responseContent, (text1, (20, 20)), (text2, (20, 50)), (text3, (20, 80)));

            return renderedImage;
        }

        private static async Task<CloudBlockBlob> GetCloudBlockBlob(BlobMessage message, CloudStorageAccount storageAccount)
        {
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
            CloudBlobContainer blobContainer = blobClient.GetContainerReference("weatherblobs");
            await blobContainer.CreateIfNotExistsAsync();
            string fileName = String.Format($"{message.Guid}.png");
            CloudBlockBlob cloudBlockBlob = blobContainer.GetBlockBlobReference(fileName);
            cloudBlockBlob.Properties.ContentType = "image/png";
            return cloudBlockBlob;
        }

        private static string GetBeerMessage(double tempInCelsius)
        {
            string beerMessage;
            if (tempInCelsius > 15.00)
            {
                beerMessage = "It's beer time!";
            }
            else
            {
                beerMessage = "It's coffee time!";
            }

            return beerMessage;
        }
    }
}
