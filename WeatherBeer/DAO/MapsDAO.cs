using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using WeatherBeer.Models;

namespace WeatherBeer.DAO
{
    class MapsDAO : Singleton<MapsDAO>
    {
        static string openWeatherApiKey = Environment.GetEnvironmentVariable("AzureMapsKey");
        static string Url = "https://atlas.microsoft.com/map";
        static string Type = "static";
        static string Format = "png";
        static string Version = "1.0";


        public async Task<HttpResponseMessage> GetMaps(Coord coord)
        {
            string mapsUrl = $"{Url}/{Type}/{Format}?subscription-key={openWeatherApiKey}&api-version={Version}&center={coord.Lon},{coord.Lat}";

            HttpClient client = new HttpClient();
            HttpResponseMessage response = await client.GetAsync(mapsUrl);

            return response;
        }
    }
}
