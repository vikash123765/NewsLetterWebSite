using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NewsLetterBanan.Data;
using NewsLetterBanan.Services.Interfaces;

namespace NewsLetterBanan.Controllers
{
   
    public class ApiController : Controller
    {
          
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ApiController> _logger;

        public ApiController(ApplicationDbContext context, ILogger<ApiController> logger)
        {
            _context = context;
            _logger = logger;


        }
        [HttpGet("GetPersonalizedNewsletterJson")]
        public async Task<IActionResult> GetPersonalizedNewsletterData()
        {
            var users = await _context.Users.ToListAsync();

            var userData = users.Select(user => new
            {
                UserName = user.FirstName + " " + user.LastName,
                Email = user.Email,

                // ✅ Check if the user has any active subscription
                HasActiveSubscription = _context.Subscriptions
                    .Any(sub => sub.User.Id == user.Id && sub.PaymentComplete),

                // ✅ Exclusive articles based on the user's active subscriptions
                ExclusiveArticles = _context.Articles
                    .Where(a => a.Exclusive && !a.IsArchived && a.IsApproved)
                    .Where(a => a.Categories.Any(c =>
                        _context.Subscriptions
                            .Where(sub => sub.User.Id == user.Id && sub.PaymentComplete)
                            .Select(sub => sub.SubscriptionType.TypeName)
                            .Contains(c.Name)))
                    .Select(a => new
                    {
                        a.Headline,
                        a.ContentSummary,
                        a.DateStamp,
                        Categories = a.Categories.Select(c => c.Name).ToList(),
                        Tags = a.Tags.Select(t => t.TagName).ToList(),
                        a.SourceURL,
                        Author = a.User.FirstName + " " + a.User.LastName
                    }).ToList(),

                // ✅ Latest Articles (excluding Exclusive)
                LatestArticles = _context.Articles
                    .Where(a => !a.Exclusive && !a.IsArchived && a.IsApproved)
                    .OrderByDescending(a => a.DateStamp)
                    .Take(3)
                    .Select(a => new
                    {
                        a.Headline,
                        a.ContentSummary,
                        a.DateStamp,
                        Categories = a.Categories.Select(c => c.Name).ToList(),
                        Tags = a.Tags.Select(t => t.TagName).ToList(),
                        a.SourceURL,
                        Author = a.User.FirstName + " " + a.User.LastName
                    }).ToList(),

                // ✅ Editor's Choice Articles (excluding Exclusive)
                EditorsChoiceArticles = _context.Articles
                    .Where(a => a.IsEditorsChoice && !a.Exclusive && !a.IsArchived && a.IsApproved)
                    .OrderByDescending(a => a.DateStamp)
                    .Take(3)
                    .Select(a => new
                    {
                        a.Headline,
                        a.ContentSummary,
                        a.DateStamp,
                        Categories = a.Categories.Select(c => c.Name).ToList(),
                        Tags = a.Tags.Select(t => t.TagName).ToList(),
                        a.SourceURL,
                        Author = a.User.FirstName + " " + a.User.LastName
                    }).ToList(),

                // ✅ Most Popular Articles (excluding Exclusive)
                MostPopularArticles = _context.Articles
                    .Where(a => !a.Exclusive && !a.IsArchived && a.IsApproved)
                    .OrderByDescending(a => _context.ArticleLikes.Count(l => l.ArticleId == a.Id))
                    .Take(3)
                    .Select(a => new
                    {
                        a.Headline,
                        a.ContentSummary,
                        a.DateStamp,
                        Categories = a.Categories.Select(c => c.Name).ToList(),
                        Tags = a.Tags.Select(t => t.TagName).ToList(),
                        a.SourceURL,
                        Author = a.User.FirstName + " " + a.User.LastName
                    }).ToList()

            }).ToList();

            return Ok(userData);
        }


        [HttpGet("GetExpiringSubscriptionsJson")]
        public string GetExpiringSubscriptionsJson()
        {
            DateTime thresholdDate = DateTime.UtcNow.AddDays(90); // 5 days from now

            var expiringSubscriptions = _context.Subscriptions
                .Where(sub => sub.Expires <= thresholdDate && sub.PaymentComplete) // Ensure payment is complete
                .Include(sub => sub.User) // Include user details
                .Include(sub => sub.SubscriptionType) // Include subscription type details
                .ToList()
                .GroupBy(sub => sub.User) // Group by user to avoid duplicates
                .Select(group => new
                {
                    UserName = group.Key.FirstName + " " + group.Key.LastName,
                    Email = group.Key.Email,
                    ExpiringSubscriptions = group.Select(sub => new
                    {
                        SubscriptionType = sub.SubscriptionType.TypeName,
                        ExpiryDate = sub.Expires
                    })
                })
                .ToList();

            return JsonSerializer.Serialize(expiringSubscriptions, new JsonSerializerOptions { WriteIndented = true });
        }
        public class ArticleView
        {
            public DateTime ViewedAt { get; set; }
        }
        // POST: api/Article/ArchiveOldArticles
        [HttpPost("ArchiveOldArticles")]
        public async Task<IActionResult> ArchiveOldArticles()
        {
            // Archive articles older than 6 minutes (adjust threshold as needed)
            var thresholdDate = DateTime.Now.AddMinutes(-2);

            var articlesToArchive = await _context.Articles
                .Where(a => a.DateStamp < thresholdDate && !a.IsArchived)
                .ToListAsync();

            if (articlesToArchive.Count == 0)
            {
                _logger.LogInformation("No articles to archive.");
                return Ok("No articles to archive.");
            }

            foreach (var article in articlesToArchive)
            {
                article.IsArchived = true;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation($"{articlesToArchive.Count} articles archived.");
            return Ok($"{articlesToArchive.Count} articles archived.");
        }

        public IActionResult Index()
        {
            return View();
        }
    }
}
