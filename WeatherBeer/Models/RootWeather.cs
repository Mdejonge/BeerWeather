using System;
using System.Collections.Generic;

namespace WeatherBeer.Models
{
    public class RootWeather
    {
        public Coord Coord { get; set; }
        public Sys Sys { get; set; }
        public List<Weather> Weather { get; set; }
        public Main Main { get; set; }
        public Wind Wind { get; set; }
        public Cloud Clouds { get; set; }
        public int Dt { get; set; }
        public int Id { get; set; }
        public string Name { get; set; }
        public int Cod { get; set; }
    } 
}
