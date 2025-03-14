using Microsoft.AspNetCore.Mvc;
using NewsLetterBanan.Services.Interfaces;
using System.Threading.Tasks;

namespace NewsLetterBanan.ViewComponents
{
    public class WeatherViewComponent : ViewComponent
    {
        private readonly IRequestService _requestService;

        public WeatherViewComponent(IRequestService requestService)
        {
            _requestService = requestService;
        }

        public async Task<IViewComponentResult> InvokeAsync(string city)
        {
            var weather = await _requestService.GetForecast(city);
            return View(weather);
        }
    }
}
