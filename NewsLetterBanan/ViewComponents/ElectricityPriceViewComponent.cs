using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using NewsLetterBanan.Models.ViewModels;
using NewsLetterBanan.Services;
using NewsLetterBanan.Services.Interfaces;

namespace NewsLetterBanan.ViewComponents
{
    public class ElectricityPriceViewComponent : ViewComponent
    {
        private readonly IRequestService _requestService;

        public ElectricityPriceViewComponent(IRequestService requestService)
        {
            _requestService = requestService;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var electricityPrices = await _requestService.GetElectricityPricesAsync();
            return View("Default", electricityPrices); // Points to the Default view in ElectricityPrice folder
        }


   
    }
}
