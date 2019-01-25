using System;
namespace WeatherBeer.Models
{
    public class BlobMessage : TriggerMessage
    {
        public RootWeather Weather { get; set; }

        public BlobMessage(RootWeather weather)
        {
            this.Weather = weather;
        }
    }
}
