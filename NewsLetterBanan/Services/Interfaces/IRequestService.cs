using System.Threading.Tasks;
using NewsLetterBanan.Models.API;

namespace NewsLetterBanan.Services.Interfaces
{
    public interface IRequestService
    {
        Task<WeatherForecast> GetForecast(string city);
        Task<ElectricityPricesViewModel> GetElectricityPricesAsync();
    }
}
