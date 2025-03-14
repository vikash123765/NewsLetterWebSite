using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using NewsLetterBanan.Models.API;
using NewsLetterBanan.Services.Interfaces;
//using ElectricityPrice = NewsLetterBanan.Models.API.ElectricityPrice;

namespace NewsLetterBanan.Services
{
    public class RequestService : IRequestService
    {
        private readonly HttpClient _httpClient;

        public RequestService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }
        public async Task<ElectricityPricesViewModel> GetElectricityPricesAsync()
        {
            var url = "https://spotprices.lexlink.se/espot";

            try
            {
                var response = await _httpClient.GetStringAsync(url);
                Console.WriteLine("Raw API Response: " + response);  // 🛠 Debugging: Check API response

                if (string.IsNullOrEmpty(response))
                {
                    Console.WriteLine("Error: API returned empty response.");
                    return null;
                }

                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

                var data = JsonSerializer.Deserialize<ElectricityPricesViewModel>(response, options);

                if (data == null)
                {
                    Console.WriteLine("Error: JSON Deserialization failed.");
                    return null;
                }

                return data;
            }
            catch (Exception ex)
            {
                Console.WriteLine("API Error: " + ex.Message);
                return null;
            }
        }


        public async Task<WeatherForecast> GetForecast(string city)
        {
            var url = $"http://weatherapi.dreammaker-it.se/forecast?city={city}&lang=en";
            return await _httpClient.GetFromJsonAsync<WeatherForecast>(url);
        }

    }
}
