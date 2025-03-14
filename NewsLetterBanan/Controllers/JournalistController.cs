using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NewsLetterBanan.Data;
using NewsLetterBanan.Models.ViewModels;
using NewsLetterBanan.Services.Interfaces;

namespace NewsLetterBanan.Controllers
{
    [Authorize(Roles = "Journalist")]

    public class JournalistController : Controller
    {
        private readonly IUserService _userService;
        private readonly ApplicationDbContext _dbContext;
        private readonly UserManager<User> _userManager;
        private readonly IJournalistService _journalistService;
        private readonly RoleManager<IdentityRole> _roleManager;
        private List<Article> exclusiveArticles;
        private readonly IAdminService _adminService;


        public JournalistController(IUserService userService, ApplicationDbContext db, UserManager<User> userManager, RoleManager<IdentityRole> roleManager, IAdminService adminService, IJournalistService journalistService)
        {
            _dbContext = db;
            _userService = userService;
            _userManager = userManager;
            _roleManager = roleManager;
            _adminService = adminService;
            _journalistService = journalistService;
        }
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet("JournalistPage")]
        public async Task<IActionResult> JournalistPage()
        {
            // Get the current user's ID (adjust according to your authentication setup)
            var userId = User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("User is not authenticated.");
            }

            // Fetch articles that belong to this user
            var articles = await _dbContext.Articles
                                .Where(a => a.UserId == userId)
                                .ToListAsync();

            return View(articles);


        }


        [HttpGet("CreateArticle")]
        public IActionResult CreateArticle()
        {
            try
            {
                // Fetch categories from your database or any other source
                var categories = _dbContext.Categories.Select(c => c.Name).ToList(); // Assuming you have a Categories table
                var tags = _dbContext.Tags.ToList(); // Fetch all tags from your database
                // Pass the tags to the ViewBag // Fetch all tags from your database
                ViewBag.Tags = tags; // Pass the tags to the ViewBag
                // Pass the categories to the view via ViewBag
                ViewBag.Categories = categories;

                // Return the view with the ViewModel
                var viewModel = new CreateArticleViewModel
                {
                    DateStamp = DateTime.Now // Default DateStamp
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                // Handle any exceptions
                Console.WriteLine($"Error in GET CreateArticle: {ex.Message}");
                return StatusCode(500, "An error occurred while loading the form.");
            }
        }
        [HttpPost("CreateArticle")]
        public IActionResult CreateArticle(Models.ViewModels.CreateArticleViewModel model)
        {
            if (!ModelState.IsValid)
            {
                foreach (var key in ModelState.Keys)
                {
                    foreach (var error in ModelState[key].Errors)
                    {
                        Console.WriteLine($"Error in {key}: {error.ErrorMessage}");
                    }
                }
                return View(model);
            }

            // Get the logged-in user's ID
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return StatusCode(403, "User is not authenticated.");
            }

            try
            {
                Console.WriteLine("Processing submitted form.");

                // Access selected categories
                var selectedCategories = model.CategoryNames;

                // Process the selected categories as needed
                foreach (var category in selectedCategories)
                {
                    Console.WriteLine($"Selected category: {category}");
                }

                // Call the service to handle the entire article creation process
                _journalistService.CreateArticleAndSave(model, userId);

                return RedirectToAction("JournalistPage");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in POST CreateArticle: {ex.Message}");
                return StatusCode(500, "An error occurred while saving the article.");
            }
        }

        [HttpGet("EditArticle/{id}")]
        public async Task<IActionResult> EditArticle(int id)
        {
            Console.WriteLine($"[GET] EditArticle called with ID: {id}");

            var viewModel = await _journalistService.GetArticleForEditAsync(id);
            if (viewModel == null)
            {
                Console.WriteLine($"[WARN] Article with ID {id} not found.");
                return NotFound();
            }

            try
            {
                var categories = _dbContext.Categories.Select(c => c.Name).ToList();
                var tags = _dbContext.Tags.ToList();

                Console.WriteLine($"[INFO] Fetched {categories.Count} categories and {tags.Count} tags from DB.");
                Console.WriteLine($"[INFO] Tags: {string.Join(", ", tags.Select(t => t.TagName))}");

                ViewBag.Categories = categories;
                ViewBag.Tags = tags;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Fetching categories/tags failed: {ex.Message}");
                return StatusCode(500, "Internal server error while fetching categories/tags.");
            }

            return View(viewModel);
        }

        [HttpPost("EditArticle/{id}")]
        public async Task<IActionResult> EditArticle(int id, CreateArticleViewModel viewModel)
        {
            Console.WriteLine($"[POST] EditArticle called with ID: {id}");

            if (!ModelState.IsValid)
            {
                Console.WriteLine("[WARN] Model validation failed. Returning view with validation errors.");
                ViewBag.Categories = _dbContext.Categories.ToList();
                ViewBag.Tags = _dbContext.Tags.ToList();
                return View(viewModel);
            }

            try
            {
                Console.WriteLine($"[INFO] Attempting to update article ID {id}.");
                var success = await _journalistService.UpdateArticleAsync(id, viewModel);

                if (success)
                {
                    Console.WriteLine($"[SUCCESS] Article ID {id} updated successfully.");
                    TempData["SuccessMessage"] = "Article updated successfully.";
                    return RedirectToAction("JournalistPage");
                }

                Console.WriteLine($"[FAIL] Article ID {id} update failed - Article not found.");
                return NotFound();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Error updating article ID {id}: {ex.Message}");
                return StatusCode(500, "Internal server error while updating article.");
            }
        }

        // POST: Editor/DeleteArticle/{id}
        [HttpPost("Journalist/DeleteArticle/{id}")]
        public async Task<IActionResult> DeleteArticle(int id)
        {
            var article = await _dbContext.Articles.FindAsync(id);
            if (article == null)
            {
                return NotFound(); // Return NotFound if the article doesn't exist
            }

            _dbContext.Articles.Remove(article); // Remove the article from the database
            await _dbContext.SaveChangesAsync();

            return RedirectToAction("JournalistPage"); // Redirect back to ManageArticles
        }
        // GET: Display all Comments and Replies for the logged-in User
        [HttpGet("/Journalist/ManageUserComments")]
        public IActionResult ManageUserComments()
        {
            // Get the currently logged-in User's ID
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            // Get Comments and their Replies for the current User
            var comments = _dbContext.Comments
                                   .Where(c => c.UserId == userId)
                                   .Include(c => c.Replies)
                                   .Include(c => c.Article)
                                   .ToList();

            return View(comments);

        }
        // POST: Delete a Comment
        [HttpPost("/Journalist/DeleteComment")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteComment(int id)
        {
            var comment = _dbContext.Comments.Include(c => c.Replies).FirstOrDefault(c => c.Id == id);

            if (comment == null)
            {
                return NotFound();
            }

            // Delete all Replies associated with the Comment
            _dbContext.CommentReplies.RemoveRange(comment.Replies);

            // Delete the Comment itself
            _dbContext.Comments.Remove(comment);
            _dbContext.SaveChanges();

            TempData["SuccessMessage"] = "Comment deleted successfully!";
            return Redirect(Request.Headers["Referer"].ToString());
        }
        // POST: Delete a Comment Reply
        [HttpPost("/Journalist/DeleteCommentReply")]
        public IActionResult DeleteCommentReply(int id)
        {
            var reply = _dbContext.CommentReplies.FirstOrDefault(r => r.Id == id);
            if (reply == null)
            {
                TempData["ErrorMessage"] = "Reply not found.";
                return Redirect(Request.Headers["Referer"].ToString());
            }

            _dbContext.CommentReplies.Remove(reply);
            _dbContext.SaveChanges();

            TempData["SuccessMessage"] = "Reply deleted successfully!";


            // Redirect ensures the page reloads with updated data
            return Redirect(Request.Headers["Referer"].ToString());
        }

        // GET: Display all comments for articles authored by this editor
        public IActionResult ManageArticleCommentsAndReplies()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var comments = _dbContext.Comments
                .Include(c => c.Article)
                .Include(c => c.Replies)
                .ThenInclude(r => r.User)  // Include User for reply
                .Include(c => c.User)      // Include User for comment
                .Where(c => c.Article.UserId == userId) // Only comments on editor's articles
                .ToList();

            return View(comments);
        }

    }
}
