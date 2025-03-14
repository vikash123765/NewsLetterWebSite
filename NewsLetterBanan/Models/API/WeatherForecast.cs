using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace NewsLetterBanan.Models.API
{
    public class WeatherForecast
    {
        public string City { get; set; }
        public int TemperatureC { get; set; }
        public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
        public int Humidity { get; set; }
        public int WindSpeed { get; set; }
        public string Summary { get; set; }  // ✅ Change from int to string
        public DateTime Date { get; set; }
    }

}
