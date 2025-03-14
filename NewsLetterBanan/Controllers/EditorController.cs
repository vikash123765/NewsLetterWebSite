using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NewsLetterBanan.Data;
using NewsLetterBanan.Models.ViewModels;
using NewsLetterBanan.Services.Interfaces;

namespace NewsLetterBanan.Controllers
{
    [Authorize(Roles = "Editor")]
    public class EditorController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IArticleService _articleService;
        private readonly IAdminService _adminService;
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public EditorController(ApplicationDbContext context, IArticleService articleService, UserManager<User> userManager, RoleManager<IdentityRole> roleManager, IAdminService adminService)
        {
            _context = context;
            _articleService = articleService;
            _userManager = userManager;
            _roleManager = roleManager;
            _adminService = adminService;
        }

        public IActionResult Index()
        {
            return View();

        }



        [HttpGet("ManageArticles")]// Explicit route definition
        public async Task<IActionResult> ManageArticles()
        {
            var articles = await _context.Articles.Include(a => a.User).ToListAsync(); // Fetch all articles
            return View(articles); // Pass articles to the view


        }


        [HttpGet("/Editor/CreateArticle")]
        public IActionResult CreateArticle()
        {
            try
            {
                // Fetch categories from your database or any other source
                var categories = _context.Categories.Select(c => c.Name).ToList(); // Assuming you have a Categories table
                var tags = _context.Tags.ToList(); // Fetch all tags from your database
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




        [HttpPost("/Editor/CreateArticle")]
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
                _adminService.CreateArticleAndSave(model, userId);

                return RedirectToAction("ManageArticles");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in POST CreateArticle: {ex.Message}");
                return StatusCode(500, "An error occurred while saving the article.");
            }
        }

        [HttpGet("/Editor/EditArticle/{id}")]
        public async Task<IActionResult> EditArticle(int id)
        {
            Console.WriteLine($"[GET] EditArticle called with ID: {id}");

            var viewModel = await _adminService.GetArticleForEditAsync(id);
            if (viewModel == null)
            {
                Console.WriteLine($"[WARN] Article with ID {id} not found.");
                return NotFound();
            }

            try
            {
                var categories = _context.Categories.Select(c => c.Name).ToList();
                var tags = _context.Tags.ToList();

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

        [HttpPost("/Editor/EditArticle/{id}")]
        public async Task<IActionResult> EditArticle(int id, CreateArticleViewModel viewModel)
        {
            Console.WriteLine($"[POST] EditArticle called with ID: {id}");

            if (!ModelState.IsValid)
            {
                Console.WriteLine("[WARN] Model validation failed. Returning view with validation errors.");
                ViewBag.Categories = _context.Categories.ToList();
                ViewBag.Tags = _context.Tags.ToList();
                return View(viewModel);
            }

            try
            {
                Console.WriteLine($"[INFO] Attempting to update article ID {id}.");
                var success = await _adminService.UpdateArticleAsync(id, viewModel);

                if (success)
                {
                    Console.WriteLine($"[SUCCESS] Article ID {id} updated successfully.");
                    TempData["SuccessMessage"] = "Article updated successfully.";
                    return RedirectToAction("ManageArticles");
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



 
    [HttpGet("/Editor/ManageComments")]
    // GET: Manage Comments
    public IActionResult ManageComments()
    {
        var comments = _context.Comments
            .Include(c => c.User) // Include user who made the comment
            .Include(c => c.Article) // Include related article
            .Include(c => c.Replies) // Include replies
                .ThenInclude(r => r.User) // Include users who replied
            .OrderByDescending(c => c.DateStamp) // Show latest comments first
            .ToList();

        return View(comments);
    }



    // POST: /Admin/DeleteArticle/{id}
    [HttpPost("DeleteArticle/{id}")]
        public async Task<IActionResult> DeleteArticle(int id)
        {
            var article = await _context.Articles.FindAsync(id);
            if (article == null)
            {
                return NotFound(); // Return NotFound if the article doesn't exist
            }

            _context.Articles.Remove(article); // Remove the article from the database
            await _context.SaveChangesAsync();
            return RedirectToAction("ManageArticles"); // Redirect back to ManageArticles
        }

        // POST: Delete Comment
        [HttpPost("DeleteComment")]
        public IActionResult DeleteComment(int id)
        {
            var comment = _context.Comments
                .Include(c => c.Replies) // Include replies to remove them as well
                .FirstOrDefault(c => c.Id == id);

            if (comment != null)
            {
                _context.Comments.Remove(comment);
                _context.SaveChanges();
                TempData["SuccessMessage"] = "Comment deleted successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = "Comment not found.";
            }

            return RedirectToAction("ManageComments");
        }

        // POST: Delete Comment Reply
        [HttpPost("DeleteCommentReply")]
        public IActionResult DeleteCommentReply(int id)
        {
            var reply = _context.CommentReplies.FirstOrDefault(r => r.Id == id);

            if (reply != null)
            {
                _context.CommentReplies.Remove(reply);
                _context.SaveChanges();
                TempData["SuccessMessage"] = "Reply deleted successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = "Reply not found.";
            }

            return RedirectToAction("ManageComments");
        }
    }
}
