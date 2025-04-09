using System.Diagnostics;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NewsLetterBanan.Data;
using NewsLetterBanan.Models;
using NewsLetterBanan.Models.API;
using NewsLetterBanan.Services;
using NewsLetterBanan.Services.Interfaces;

namespace NewsLetterBanan.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;
        private readonly IRequestService _requestService;
        private readonly UserManager<User> _userManager;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context, IUserService userService, IRequestService requestService, UserManager<User> userManager)
        {
            _logger = logger;
            _context = context;
            _requestService = requestService;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            // Get latest articles (excluding Exclusive)
            var latestArticles = await _context.Articles
                .Include(a => a.Categories)
                .Include(a => a.Images)
                .Include(a => a.Tags)
                .Include(a => a.User)

                .Where(a => !a.Exclusive && !a.IsArchived && a.IsApproved) // Exclude Exclusive articles
                .OrderByDescending(a => a.DateStamp)
                .Take(5)
                .ToListAsync();

            // Get Editor's Choice articles (excluding Exclusive)
            var editorsChoice = await _context.Articles
                .Include(a => a.Categories)
                .Include(a => a.Images)
                .Include(a => a.Tags)
                .Include(a => a.User)
                .Where(a => a.IsEditorsChoice && !a.Exclusive && !a.IsArchived && a.IsApproved) // Exclude Exclusive articles
                .OrderByDescending(a => a.DateStamp)
                .ToListAsync();

            // Get Most Popular articles based on like count (excluding Exclusive)
            var mostPopular = await _context.Articles
                .Include(a => a.Categories)
                .Include(a => a.Images)
                .Include(a => a.Tags)
                .Include(a => a.User)
                .Where(a => !a.Exclusive && !a.IsArchived && a.IsApproved) // Exclude Exclusive articles
                .OrderByDescending(a => _context.ArticleLikes.Count(l => l.ArticleId == a.Id)) // Sort by like count
                .Take(5)
                .ToListAsync();

            // Fetch statistics
            ViewBag.CommentCounts = await _context.Comments
                .GroupBy(c => c.ArticleId)
                .ToDictionaryAsync(g => g.Key, g => g.Count());

            ViewBag.LikeCounts = await _context.ArticleLikes
                .GroupBy(l => l.ArticleId)
                .ToDictionaryAsync(g => g.Key, g => g.Count());
            // *** Add view count statistics ***
            ViewBag.ViewCounts = await _context.Articles
                .ToDictionaryAsync(a => a.Id, a => a.Views);

            ViewBag.IsLoggedIn = User.Identity.IsAuthenticated;
            // Fetch the current logged-in user's ID
            var userId = _userManager.GetUserId(User);

            var userArticleLikes = await _context.ArticleLikes
        .Where(l => l.UserId == userId)
        .ToDictionaryAsync(l => l.ArticleId, l => true);

            ViewBag.IsLiked = userArticleLikes;

            // Prepare the ViewModel
            var viewModel = new NewsLetterBanan.Models.ViewModels.HomePageViewModel
            {
                Latest = latestArticles,
                EditorsChoice = editorsChoice,
                MostPopular = mostPopular,  // Add Most Popular articles
                ElectricityPrices = new List<ElectricityPrice>() // Assuming this gets populated elsewhere
            };

            return View(viewModel);
        }

        public IActionResult TextToImage()
        {
            return View("TextToImage"); // This would return the view from Views/Shared/TextToImage.cshtml
        }


        public async Task<IActionResult> LoadElectricityComponent()
        {
            return ViewComponent("ElectricityPrice");
        }

        public async Task<IActionResult> LoadWeatherComponent(string city)
        {
            if (string.IsNullOrEmpty(city))
            {
                // If the city is empty, show an error
                ViewData["ErrorMessage"] = "Please enter a valid city.";
                return View("Error");
            }

            // Make the API call to check if the city exists
            var weatherData = await _requestService.GetForecast(city);

            if (weatherData == null)
            {
                // If the city doesn't exist, handle it
                ViewData["ErrorMessage"] = "City does not exist. Please try again.";
                return View("Error");
            }

            // If the city exists, load the weather component
            return ViewComponent("Weather", new { city });
        }

        public IActionResult About()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
