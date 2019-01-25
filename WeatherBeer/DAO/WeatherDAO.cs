using System;
using System.Net.Http;
using System.Threading.Tasks;

using Newtonsoft.Json;
using WeatherBeer.Models;

namespace WeatherBeer
{
    class WeatherDAO : Singleton<WeatherDAO>
    {
        static string openWeatherApiKey = Environment.GetEnvironmentVariable("OpenWeatherApiKey");
        static string url = "https://api.openweathermap.org/data";
        static string version = "2.5";
        static string type = "weather";
        static string units = "metric";

        public async Task<RootWeather> GetWeather(string city, string country)
        {
            string openWeatherUrl = $"{url}/{version}/{type}?q={city},{country}&appid={openWeatherApiKey}&units={units}";

            HttpClient Client = new HttpClient();
            HttpResponseMessage Response = await Client.GetAsync(openWeatherUrl);
            string content = await Response.Content.ReadAsStringAsync();

            RootWeather weather = (RootWeather)JsonConvert.DeserializeObject(content, typeof(RootWeather));

            return weather;
        }
    }
}
