
using System.Text.Json.Serialization;

namespace NewsLetterBanan.Models.API
{
    public class ElectricityPricesViewModel
    {
        public string Date { get; set; }
        public List<ElectricityPrice> SE1 { get; set; }
        public List<ElectricityPrice> SE2 { get; set; }
        public List<ElectricityPrice> SE3 { get; set; }
        public List<ElectricityPrice> SE4 { get; set; }


    }

    public class ElectricityPrice
    {
        public int Hour { get; set; }

        [JsonPropertyName("price_eur")]
        public double PriceEur { get; set; }

        [JsonPropertyName("price_sek")]
        public double PriceSek { get; set; }

        public int KMeans { get; set; }

        public string SE { get; set; }   // Optional, only if API returns this

        public string Date { get; set; } // Optional, only if API returns this
    }



}
