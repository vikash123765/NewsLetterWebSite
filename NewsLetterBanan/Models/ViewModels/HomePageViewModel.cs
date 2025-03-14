using NewsLetterBanan.Data;
using NewsLetterBanan.Models.API;

namespace NewsLetterBanan.Models.ViewModels
{
    public class HomePageViewModel
{
    //   public WeatherData Weather { get; set; }
    public IEnumerable<Article> Latest { get; set; }
    public IEnumerable<Article> EditorsChoice { get; set; }

        public IEnumerable<Article> MostPopular { get; set; }

        public List<ElectricityPrice> ElectricityPrices { get; set; }  // Add this property
    }
}

