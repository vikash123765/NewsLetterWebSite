using System.Drawing.Printing;
using System.Globalization;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NewsLetterBanan.Data;
using NewsLetterBanan.Services.Interfaces;
using Microsoft.CognitiveServices.Speech;
using Newtonsoft.Json;
using System.Text;
using NewsLetterBanan.Models;
using Newtonsoft.Json.Linq;
using Azure;
using Azure.AI.TextAnalytics;
using Azure.AI.OpenAI;
using OpenAI.Images;

namespace NewsLetterBanan.Controllers
{

    public class ArticleController : Controller
    {
        private readonly ApplicationDbContext _context;

        private readonly ILogger<ArticleController> _logger;
        private readonly IArticleService _articleService;
        private readonly UserManager<User> _userManager;
      
        private readonly RoleManager<IdentityRole> _roleManager;
        private static readonly string key = "RMYzPGNIROnY8yH39wv6jd3YBquqJ8FUcG4PtWoXHggke5O0yC1FJQQJ99BCACfhMk5XJ3w3AAAbACOGUVrS";
        private static readonly string endpoint = "https://api.cognitive.microsofttranslator.com";
        private static readonly string location = "swedencentral";
        // This example requires environment variables named "LANGUAGE_KEY" and "LANGUAGE_ENDPOINT"
        static string languageKey = "Dyn05PviUzAQGOfRkhFTHzp9hc1ZPrCzcjZP5YXYf8GXLOlkz0daJQQJ99BCACfhMk5XJ3w3AAAaACOG7rVS";
        static string languageEndpoint = "https://gr2409languageservice.cognitiveservices.azure.com/";

        private static readonly AzureKeyCredential credentials = new AzureKeyCredential(languageKey);
        private static readonly Uri endpointL = new Uri(languageEndpoint);

        private static readonly TextAnalyticsClient textAnalyticsClient = new TextAnalyticsClient(endpointL, credentials);

        private static string speechKey = "8iKRLUYJQEzLjKqmTLQ43o911X85h9VUOeVsuy5fmllzJhV2SUMLJQQJ99BCACYeBjFXJ3w3AAAYACOGoTw3";
        private static string speechRegion = "eastus";


        public ArticleController(ApplicationDbContext context, IArticleService articleService, UserManager<User> userManager, RoleManager<IdentityRole> roleManager, ILogger<ArticleController> logger)
        {
            _context = context;
            _articleService = articleService;
            _userManager = userManager;
            _roleManager = roleManager;
            _logger = logger;
          
        }

        [HttpGet("GetAllUsersAsync")]

        public async Task<IActionResult> GetAllUsersAsync()
        {
            var users = await _userManager.Users.ToListAsync();
            return Ok(users); // Wraps the list in an HTTP 200 OK response.
        }
        [HttpPost("AssignRole")]
        public async Task<IActionResult> AssignRole(string UserId, string RoleName)
        {
            var user = await _userManager.FindByIdAsync(UserId);
            if (user == null)
            {
                return NotFound("User not found.");
            }

            var roleExist = await _roleManager.RoleExistsAsync(RoleName);
            if (!roleExist)
            {
                await _roleManager.CreateAsync(new IdentityRole(RoleName));
            }

            // Check if the user is already assigned to the role
            if (await _userManager.IsInRoleAsync(user, RoleName))
            {
                return BadRequest($"User {user.UserName} is already assigned to the {RoleName} role.");
            }

            var result = await _userManager.AddToRoleAsync(user, RoleName);
            if (result.Succeeded)
            {
                return Ok($"User {user.UserName} has been assigned the {RoleName} role.");
            }

            return BadRequest("Error assigning role.");
        }



        public async Task<IActionResult> GetAllArticles(int? categoryId, string? searchKeyword, string? sortBy, int? page = 1)
        {
            int pageSize = 5;
            int currentPage = page ?? 1;
            if (currentPage < 1) currentPage = 1;

            ViewBag.Categories = await _context.Categories.ToListAsync();
            // Fetch all archived articles for the dropdown
            ViewBag.ArchivedArticles = await _context.Articles
                .Where(a => a.IsArchived && a.IsApproved)
                .Select(a => new { a.Id, a.Headline }) // Fetch only necessary fields
                .ToListAsync();

            // Base query
            var query = _context.Articles
                .Include(a => a.Categories)
                .Include(a => a.Images)
                .Include(a => a.User)
                .Include(a => a.Tags)
                .Include(a => a.Comments)
                    .ThenInclude(c => c.User)
                .Include(a => a.Comments)
                    .ThenInclude(c => c.Replies)
                .Include(a => a.Comments)
                    .ThenInclude(c => c.CommentLikes)
                 .Where(a => !a.Exclusive && !a.IsArchived && a.IsApproved) // 🔥 Excluding unapproved articles

                .AsQueryable();

            // Apply filtering
            if (categoryId.HasValue && categoryId > 0)
            {
                query = query.Where(a => a.Categories.Any(c => c.Id == categoryId.Value));
            }

            if (!string.IsNullOrEmpty(searchKeyword))
            {
                query = query.Where(a => a.Headline.Contains(searchKeyword) || a.Content.Contains(searchKeyword));
            }

            // **Get total count before applying pagination**
            int totalArticles = await query.CountAsync();

            // Apply sorting BEFORE pagination
            switch (sortBy)
            {
                case "date_newest":
                    query = query.OrderByDescending(a => a.DateStamp);
                    break;
                case "date_oldest":
                    query = query.OrderBy(a => a.DateStamp);
                    break;
                case "alphabetical_asc":
                    query = query.OrderBy(a => a.Headline);
                    break;
                case "alphabetical_desc":
                    query = query.OrderByDescending(a => a.Headline);
                    break;
                case "views":
                    query = query.OrderByDescending(a => a.Views);
                    break;
                case "likes":
                    query = query.OrderByDescending(a => _context.ArticleLikes.Count(l => l.ArticleId == a.Id));
                    break;
                default:
                    query = query.OrderByDescending(a => a.DateStamp);
                    break;
            }

            // Debugging: Log the sorted query
            var sortedQuery = query.ToList();
            Console.WriteLine("Sorted Query: " + string.Join(", ", sortedQuery.Select(a => a.Headline)));

            // Apply pagination AFTER sorting
            var allArticles = await query
                .Skip((currentPage - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(); // 🔥 Now we're executing the query only once!

            if (totalArticles == 0)
            {
                ViewBag.TotalPages = 1;
                ViewBag.SortBy = sortBy;
                return View(new List<Article>());
            }

            // **Sort comments and replies**
            foreach (var article in allArticles)
            {
                if (article.Comments != null)
                {
                    article.Comments = article.Comments.OrderByDescending(c => c.DateStamp).ToList();
                    foreach (var comment in article.Comments)
                    {
                        if (comment.Replies != null)
                        {
                            comment.Replies = comment.Replies.OrderByDescending(r => r.DateStamp).ToList();
                        }
                    }
                }
            }

            // Fetch statistics
            ViewBag.CommentCounts = await _context.Comments
                .GroupBy(c => c.ArticleId)
                .ToDictionaryAsync(g => g.Key, g => g.Count());

            ViewBag.LikeCounts = await _context.ArticleLikes
                .GroupBy(l => l.ArticleId)
                .ToDictionaryAsync(g => g.Key, g => g.Count());

            ViewBag.ReplyCounts = await _context.CommentReplies
                .GroupBy(r => r.CommentId)
                .ToDictionaryAsync(g => g.Key, g => g.Count());

            ViewBag.CommentLikeCounts = await _context.CommentLikes
                .GroupBy(cl => cl.CommentId)
                .ToDictionaryAsync(g => g.Key, g => g.Count());

            ViewBag.ReplyLikeCounts = await _context.CommentReplyLikes
                .GroupBy(rl => rl.CommentReplyId)
                .ToDictionaryAsync(g => g.Key, g => g.Count());

            ViewBag.ViewCounts = await _context.Articles
                .ToDictionaryAsync(a => a.Id, a => a.Views);

            var userId = _userManager.GetUserId(User);

            // Fetch liked articles for user
            var userLikes = await _context.ArticleLikes
                .Where(l => l.UserId == userId)
                .ToDictionaryAsync(l => l.ArticleId, l => true);
            ViewBag.IsLiked = userLikes;

            // Fetch liked comments for user
            var userCommentLikes = await _context.CommentLikes
                .Where(l => l.UserId == userId)
                .ToDictionaryAsync(l => l.CommentId, l => true);
            ViewBag.IsCommentLiked = userCommentLikes;

            var userReplyLikes = await _context.CommentReplyLikes
                .Where(l => l.UserId == userId)
                .ToDictionaryAsync(l => l.CommentReplyId, l => true);
            ViewBag.IsReplyLiked = userReplyLikes;

            ViewData["UserId"] = userId;
            ViewBag.IsLoggedIn = User.Identity.IsAuthenticated;
            ViewBag.CommentsOnOff = allArticles.FirstOrDefault()?.CommentsOnOff ?? false;
            ViewBag.SelectedCategory = categoryId;
            ViewBag.SearchKeyword = searchKeyword;
            ViewBag.SortBy = sortBy;

            // Pagination data
            ViewBag.CurrentPage = currentPage;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalArticles / pageSize);

            return View(allArticles);
        }
        [HttpGet("GetArchivedArticle")]
        public async Task<IActionResult> GetArchivedArticle(int id)
        {
            var article = await _context.Articles
                .Include(a => a.Categories)
                .Include(a => a.Images)
                .Include(a => a.User)
                .Include(a => a.Tags)
                .Include(a => a.Comments)
                    .ThenInclude(c => c.User)
                .Include(a => a.Comments)
                    .ThenInclude(c => c.Replies)
                .Include(a => a.Comments)
                    .ThenInclude(c => c.CommentLikes)
                .Include(a => a.ArticleLikes)
                .FirstOrDefaultAsync(a => a.Id == id && a.IsArchived);

            if (article == null)
            {
                return NotFound();
            }

            return RedirectToAction("ViewArticle", new { id = article.Id });

        }

        [HttpGet("ViewArticle")]
        public async Task<IActionResult> ViewArticle(int id)
        {
            // Retrieve the article by its ID with all the necessary related data
            var article = await _context.Articles
                .Include(a => a.Categories)
                .Include(a => a.Images)
                .Include(a => a.User)
                .Include(a => a.Tags)
                .Include(a => a.Comments)
                    .ThenInclude(c => c.User)
                .Include(a => a.Comments)
                    .ThenInclude(c => c.Replies)
                .Include(a => a.Comments)
                    .ThenInclude(c => c.CommentLikes)
                .Where(a => a.Id == id) // Only fetch the article with the given ID
                .FirstOrDefaultAsync();

            if (article == null)
            {
                return NotFound(); // Return a 404 if article not found
            }
            // Order the comments and replies (newest first)
            if (article.Comments != null)
            {
                article.Comments = article.Comments.OrderByDescending(c => c.DateStamp).ToList();
                foreach (var comment in article.Comments)
                {
                    if (comment.Replies != null)
                    {
                        comment.Replies = comment.Replies.OrderByDescending(r => r.DateStamp).ToList();
                    }
                }
            }
            // Retrieve the list of viewed articles from session
            var viewedArticlesJson = HttpContext.Session.GetString("viewedArticles");
            HashSet<int>? viewedArticles = string.IsNullOrEmpty(viewedArticlesJson)
                ? new HashSet<int>()
                : JsonConvert.DeserializeObject<HashSet<int>>(viewedArticlesJson);

            // If this article hasn't been viewed in this session, increment the view count
            if (!viewedArticles.Contains(id))
            {
                article.Views++;
                _context.Articles.Update(article);
                await _context.SaveChangesAsync();

                // Add the article ID to the session
                viewedArticles.Add(id);
                HttpContext.Session.SetString("viewedArticles", JsonConvert.SerializeObject(viewedArticles));
            }

            // Fetch statistics for the article
            var commentCounts = await _context.Comments
                .Where(c => c.ArticleId == id)
                .GroupBy(c => c.ArticleId)
                .ToDictionaryAsync(g => g.Key, g => g.Count());

            var likeCounts = await _context.ArticleLikes
                .Where(l => l.ArticleId == id)
                .GroupBy(l => l.ArticleId)
                .ToDictionaryAsync(g => g.Key, g => g.Count());

            var replyCounts = await _context.CommentReplies
                .Where(r => r.Comment.ArticleId == id)
                .GroupBy(r => r.CommentId)
                .ToDictionaryAsync(g => g.Key, g => g.Count());

            var commentLikeCounts = await _context.CommentLikes
                .Where(cl => cl.Comment.ArticleId == id)
                .GroupBy(cl => cl.CommentId)
                .ToDictionaryAsync(g => g.Key, g => g.Count());

            var replyLikeCounts = await _context.CommentReplyLikes
                .Where(rl => rl.CommentReply.Comment.ArticleId == id)
                .GroupBy(rl => rl.CommentReplyId)
                .ToDictionaryAsync(g => g.Key, g => g.Count());

            // Fetch the current logged-in user's ID
            var userId = _userManager.GetUserId(User);



            // Check if user has liked each comment
            var userCommentLikes = await _context.CommentLikes
                .Where(l => l.UserId == userId)
                .ToDictionaryAsync(l => l.CommentId, l => true);


            ViewBag.IsCommentLiked = userCommentLikes;


            var userReplyLikes = await _context.CommentReplyLikes
       .Where(l => l.UserId == userId)
       .ToDictionaryAsync(l => l.CommentReplyId, l => true); // Use CommentReplyId instead of Id

            ViewBag.IsReplyLiked = userReplyLikes;



            ViewBag.ViewCount = article.Views;
            // Fetch categories for the dropdown (for a consistent look in the view)
            ViewBag.Categories = await _context.Categories.ToListAsync();


            ViewData["UserId"] = userId;


            var userArticleLikes = await _context.ArticleLikes
        .Where(l => l.UserId == userId)
        .ToDictionaryAsync(l => l.ArticleId, l => true);

            ViewBag.IsLiked = userArticleLikes;



            ViewBag.IsLoggedIn = User.Identity.IsAuthenticated;

            // Pass the article and the statistics data to the view
            ViewData["ArticleId"] = article.Id; // Pass the Id to ViewData
            ViewBag.CommentCounts = commentCounts;
            ViewBag.LikeCounts = likeCounts;
            ViewBag.ReplyCounts = replyCounts;
            ViewBag.CommentLikeCounts = commentLikeCounts;
            ViewBag.ReplyLikeCounts = replyLikeCounts;
            ViewBag.CommentsOnOff = article.CommentsOnOff; // Adjust this based on your actual data model

            return View(article); // Pass the article to the view
        }


        public async Task<IActionResult> SpeakArticle(int id, string source, string? content)
        {
            try
            {
                var article = _articleService.GetArticleById(id); // Fetch the article from the database
                if (article == null || string.IsNullOrWhiteSpace(article.Content))
                {
                    return NotFound("Article not found or has no content.");
                }

                string contentToRead = article.Content;
                if (source == "home" || source == "myPage")
                {
                    contentToRead = article.Content.Length > 200 ? article.Content.Substring(0, 200) + "..." : article.Content;
                }
                else if (source == "getAllArticles")
                {
                    contentToRead = article.Content.Length > 300 ? article.Content.Substring(0, 300) + "..." : article.Content;
                }
                else if (source == "viewArticle")
                {
                    contentToRead = article.Content; // Full content
                }
                else if (source == "TranslatedArticle" && !string.IsNullOrWhiteSpace(content))
                {
                    contentToRead = content; // Full content
                }

                var speechConfig = SpeechConfig.FromSubscription(speechKey, speechRegion);
                speechConfig.SpeechSynthesisVoiceName = "en-US-AvaMultilingualNeural";

                using (var synthesizer = new SpeechSynthesizer(speechConfig, null))
                {
                    using (var result = await synthesizer.SpeakTextAsync(contentToRead))
                    {
                        if (result.Reason == ResultReason.SynthesizingAudioCompleted)
                        {
                            var audioStream = new MemoryStream(result.AudioData);
                            return File(audioStream, "audio/wav");
                        }
                        else
                        {
                            Console.WriteLine("Speech synthesis failed.");
                            return BadRequest("Failed to synthesize speech.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in SpeakArticle: {ex.Message}");
                return StatusCode(500, "Internal Server Error.");
            }
        }


        [HttpPost]
        public async Task<IActionResult> TranslateArticle(int id, string language)
        {
            var article = _context.Articles.FirstOrDefault(a => a.Id == id);
            if (article == null) return NotFound();

            string route = $"/translate?api-version=3.0&to={language}";
            object[] body = new object[] { new { Text = article.Content } };
            var requestBody = JsonConvert.SerializeObject(body);

            using (var client = new HttpClient())
            using (var request = new HttpRequestMessage(HttpMethod.Post, new Uri(endpoint + route)))
            {
                request.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");
                request.Headers.Add("Ocp-Apim-Subscription-Key", key);
                request.Headers.Add("Ocp-Apim-Subscription-Region", location);

                HttpResponseMessage response = await client.SendAsync(request);
                string result = await response.Content.ReadAsStringAsync();

                dynamic translationResult = JsonConvert.DeserializeObject(result);
                string translatedText = translationResult[0]?.translations[0]?.text;

                if (string.IsNullOrEmpty(translatedText))
                {
                    return RedirectToAction("Index"); // Fallback if translation failed
                }

                // Store the translated text in Session
                HttpContext.Session.SetString("TranslatedText", translatedText);
                HttpContext.Session.SetString("ArticleName", article.Headline ?? "Unknown Article");
                HttpContext.Session.SetString("TargetLanguage", language ?? "Unknown Language");
                HttpContext.Session.SetInt32("ArticleId", id);

                // Verify that session variables are actually stored
                if (HttpContext.Session.GetString("TranslatedText") == null)
                {
                    return Content("Session storage failed! Please try again.");
                }

                // Redirect to the TranslatedArticle view
                return RedirectToAction("TranslatedArticle");
            }
        }
        public IActionResult TranslatedArticle()
        {
            string translatedText = HttpContext.Session.GetString("TranslatedText");
            if (string.IsNullOrEmpty(translatedText))
            {
                return RedirectToAction("Index"); // Redirect if no data in session
            }

            string articleName = HttpContext.Session.GetString("ArticleName") ?? "Unknown Article";
            string targetLanguage = HttpContext.Session.GetString("TargetLanguage") ?? "Unknown Language";
            int? articleId = HttpContext.Session.GetInt32("ArticleId"); // Get stored Article ID ✅

            // 🛑 Clear session variables to avoid reloading issues
            HttpContext.Session.Remove("TranslatedText");
            HttpContext.Session.Remove("ArticleName");
            HttpContext.Session.Remove("TargetLanguage");
            HttpContext.Session.Remove("ArticleId");

            return View("TranslatedArticle", new Tuple<string, string, string, int>(translatedText, articleName, targetLanguage, articleId ?? 0));
        }

        public async Task<IActionResult> Summarize(int id)
        {
            // Fetch the actual article from your data source (replace with real implementation)
            var article= _context.Articles.FirstOrDefault(a => a.Id == id);

            string articleContent = article.Content;

            if (string.IsNullOrEmpty(articleContent))
            {
                return NotFound("Article not found.");
            }

            // ✅ No need to create a new client every time - Use the global one
            var batchInput = new List<string> { articleContent };
            var actions = new TextAnalyticsActions()
            {
                ExtractiveSummarizeActions = new List<ExtractiveSummarizeAction>
                {
                    new ExtractiveSummarizeAction() { MaxSentenceCount = 5 }
                }
            };

            // Call Text Analytics API
            AnalyzeActionsOperation operation = await textAnalyticsClient.StartAnalyzeActionsAsync(batchInput, actions);
            await operation.WaitForCompletionAsync();

            List<string> summarySentences = new List<string>();

            await foreach (AnalyzeActionsResult documentsInPage in operation.Value)
            {
                foreach (ExtractiveSummarizeActionResult summaryActionResults in documentsInPage.ExtractiveSummarizeResults)
                {
                    if (!summaryActionResults.HasError)
                    {
                        foreach (ExtractiveSummarizeResult documentResults in summaryActionResults.DocumentsResults)
                        {
                            if (!documentResults.HasError)
                            {
                                foreach (ExtractiveSummarySentence sentence in documentResults.Sentences)
                                {
                                    summarySentences.Add(sentence.Text);
                                }
                            }
                        }
                    }
                }
            }

            // Send summary sentences to the view
            ViewBag.SummarySentences = summarySentences;
            
            return View();
        }
       
        [HttpPost]
        public async Task<IActionResult> GenerateImage(string prompt)
        {
            string endpoint = "https://gr2409openai.openai.azure.com/";
            string key = "8dDGgCGBiwQAfLjRUHTK63ai0fZO9WDJzIr1uOrzeC2abbO6M5uoJQQJ99BCACYeBjFXJ3w3AAABACOGtcmi";
            // Create the OpenAI client using AzureKeyCredential
            AzureOpenAIClient openAIClient = new AzureOpenAIClient(new Uri(endpoint), new AzureKeyCredential(key));

            // This must match the custom deployment name you chose for your model
            ImageClient chatClient = openAIClient.GetImageClient("dall-e-3");

            var imageGeneration = await chatClient.GenerateImageAsync(
                    prompt,
                    new ImageGenerationOptions()
                    {
                        Size = GeneratedImageSize.W1024xH1024
                    }
                );


            string imageUrl = imageGeneration.Value.ImageUri.ToString();

            // Return the view with the image URL as the model
            return Json(new { imageUrl });
        }

        // For GET: display the form
        [HttpGet]
        public IActionResult GenerateImageForm()
        {
            return View();
        }

    }
}