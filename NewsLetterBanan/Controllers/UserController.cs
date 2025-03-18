using System.ComponentModel.Design;
using System.Security.Claims;
using Azure.Core;
using Elfie.Serialization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using NewsLetterBanan.Data;
using NewsLetterBanan.Models.ViewModels;
using NewsLetterBanan.Services;
using NewsLetterBanan.Services.Interfaces;
using NewsLetterBanan.ViewModels;
using Newtonsoft.Json;

namespace NewsLetterBanan.Controllers
{
    [Authorize(Roles = "RegUser")]

    [Route("User")]

    public class UserController : Controller
    {
        private readonly SignInManager<User> _signInManager;

        private readonly IUserService _userService;
        private readonly ApplicationDbContext _dbContext;
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private List<Article> exclusiveArticles;
        private readonly IAdminService _adminService;
        public UserController(IUserService userService, ApplicationDbContext db, UserManager<User> userManager, RoleManager<IdentityRole> roleManager, IAdminService adminService, SignInManager<User> signInManager)
        {
            _dbContext = db;
            _userService = userService;
            _userManager = userManager;
            _roleManager = roleManager;
            _adminService = adminService;
            _signInManager = signInManager;
        }


        // Index method that returns the view with the action forms
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }


        // ✅ GET: Show the subscription form
        [HttpGet("CreateSubscriptionForm")]
        public IActionResult CreateSubscriptionForm()
        {
            Console.WriteLine("📌 [GET] CreateSubscriptionForm called"); // Debug Log
            ViewBag.SubscriptionTypes = new SelectList(_dbContext.SubscriptionTypes, "Id", "TypeName");
            return View();
        }

        [HttpPost("CreateSubscriptionForm")]
        public async Task<IActionResult> CreateSubscriptionForm(int subscriptionTypeId, string creditCardNumber)
        {
            Console.WriteLine($"📌 [POST] CreateSubscriptionForm called with SubscriptionTypeId: {subscriptionTypeId}, CreditCardNumber: {creditCardNumber}");

            // Check if _dbContext is null
            if (_dbContext == null)
            {
                Console.WriteLine("❌ ERROR: DbContext is null!");
                return StatusCode(500, "Database context is unavailable.");
            }

            // Step 1: Fetch selected subscription type from DB
            var subscriptionType = await _dbContext.SubscriptionTypes.FindAsync(subscriptionTypeId);
            if (subscriptionType == null)
            {
                Console.WriteLine("❌ ERROR: SubscriptionType not found in DB");
                return NotFound("Subscription type not found.");
            }
            // Step 2: Check if the user already has an active subscription of the same type
            string userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                Console.WriteLine("❌ ERROR: User not logged in");
                return Unauthorized("User must be logged in to subscribe.");
            }

            var existingSubscription = await _dbContext.Subscriptions
                .FirstOrDefaultAsync(s => s.UserId == userId && s.SubscriptionTypeId == subscriptionTypeId);

            if (existingSubscription != null)
            {
                if (existingSubscription.Expires > DateTime.Now)
                {
                    // If the existing subscription has not expired
                    Console.WriteLine($"❌ ERROR: User already has an active subscription of type {subscriptionType.TypeName}");

                    // Return JavaScript alert telling the user they already have a subscription of this type
                    return Content("<script>alert('You already have an active subscription of this type. Please choose another type.'); window.history.back();</script>", "text/html");
                }
                else
                {
                    // If the existing subscription has expired, allow them to subscribe again
                    Console.WriteLine($"✅ User's previous subscription of type {subscriptionType.TypeName} has expired. Allowing renewal.");

                    // Optionally, you can adjust the expiration date (e.g., extend it for another 3 months)
                    existingSubscription.Expires = DateTime.Now.AddMonths(3);
                    _dbContext.Subscriptions.Update(existingSubscription);
                    await _dbContext.SaveChangesAsync();

                    // Proceed to payment and other steps
                }
            }
            else
            {
                // If the user does not have any previous subscription of this type, allow them to subscribe
                Console.WriteLine($"✅ No existing subscription found. Allowing new subscription of type {subscriptionType.TypeName}");
            }

            // Step 2: Validate Credit Card Format (Simple check for now)
            creditCardNumber = creditCardNumber.Replace(" ", ""); // Remove spaces
            if (!IsValidCreditCard(creditCardNumber))
            {
                Console.WriteLine("❌ ERROR: Invalid credit card format");
                ModelState.AddModelError("CreditCardNumber", "Invalid credit card number format.");
                return Content("<script>alert('Incorrect credit card info. Please try again.'); window.history.back();</script>", "text/html");

            }

            // Step 3: Check for any ModelState errors
            if (!ModelState.IsValid)
            {
                // Log any ModelState errors
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    Console.WriteLine($"❌ ModelState Error: {error.ErrorMessage}");
                }

                // Repopulate ViewBag with subscription types and return the view
                ViewBag.SubscriptionTypes = new SelectList(_dbContext.SubscriptionTypes, "Id", "TypeName");
                return View();
            }

            // Step 4: Get the logged-in user's ID

            if (string.IsNullOrEmpty(userId))
            {
                Console.WriteLine("❌ ERROR: User not logged in");
                return Unauthorized("User must be logged in to subscribe.");
            }

            // Step 5: Create a new subscription object
            var subscription = new Subscription
            {
                SubscriptionTypeId = subscriptionTypeId, // Use the SubscriptionTypeId here
                SubscriptionType = subscriptionType,     // Assign the subscription type here
                Created = DateTime.Now,
                Expires = DateTime.Now.AddMonths(3), // 3 months from today
                Price = subscriptionType.Price,
                PaymentComplete = false, // Initially set to false
                UserId = userId // Store user ID
            };

            Console.WriteLine($"✅ Subscription object created: {subscription.SubscriptionType.TypeName}, UserID: {subscription.UserId}");

            // Step 6: Mock payment process
            subscription.PaymentComplete = ProcessPayment(creditCardNumber);
            Console.WriteLine($"📌 PaymentComplete status: {subscription.PaymentComplete}");

            // After payment is successful
            if (subscription.PaymentComplete)
            {
                // Log the saving process
                Console.WriteLine("✅ Payment successful, saving to DB");
                _dbContext.Subscriptions.Add(subscription);
                await _dbContext.SaveChangesAsync();
                Console.WriteLine("✅ Subscription successfully saved to DB");


                // Redirect to SubscriptionSuccess
                return RedirectToAction("SubscriptionSuccess");
            }


            // If payment fails, show an error
            Console.WriteLine("❌ ERROR: Payment failed");
            ModelState.AddModelError("", "Payment failed. Please check your payment details.");

            // Repopulate ViewBag before returning to View
            ViewBag.SubscriptionTypes = new SelectList(_dbContext.SubscriptionTypes, "Id", "TypeName");

            // Log ModelState errors again
            foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
            {
                Console.WriteLine($"❌ ModelState Error: {error.ErrorMessage}");
            }

            return View();
        }



        // ✅ Simple function to check if the credit card number is exactly 16 digits
        private bool IsValidCreditCard(string creditCardNumber)
        {
            return !string.IsNullOrEmpty(creditCardNumber) &&
                   creditCardNumber.Length == 16 &&
                   creditCardNumber.All(char.IsDigit);
        }

        // ✅ Dummy payment process (always successful for now)
        private bool ProcessPayment(string creditCardNumber)
        {
            return true; // Simulate successful payment
        }

        // ✅ Success Page
        // ✅ Success Page
        [HttpGet("SubscriptionSuccess")]
        public IActionResult SubscriptionSuccess()
        {

            return View(); // Pass the latest subscription to the view
        }








        [HttpGet("GetActiveSubscriptions")]
        public async Task<IActionResult> GetActiveSubscriptions(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound("User not found.");
            }

            var activeSubscriptions = await _dbContext.Subscriptions
                .Where(s => s.UserId == userId && s.Expires > DateTime.UtcNow) // Active if not expired
                .ToListAsync();

            return Ok(activeSubscriptions);
        }

        [HttpGet("MyPage")]
        public async Task<IActionResult> MyPage()
        {
            Console.WriteLine("📌 [GET] MyPage called");

            var user = await _userManager.GetUserAsync(User); // Get logged-in user
            if (user == null)
            {
                Console.WriteLine("❌ ERROR: User not logged in");
                return Unauthorized("User is not logged in.");
            }

            string userId = user.Id; // Avoid redundant lookups

            // Fetch the user's active subscription names in a single query
            var userSubscriptionNames = await _dbContext.Subscriptions
                .Where(s => s.UserId == userId && s.PaymentComplete && s.Expires > DateTime.UtcNow)
                .Select(s => s.SubscriptionType.TypeName)
                .ToListAsync();

            // Fetch exclusive articles that match the user's active subscriptions
            var exclusiveArticles = await _dbContext.Articles
                .Where(a => a.Exclusive && !a.IsArchived && a.IsApproved &&  a.Categories.Any(c => userSubscriptionNames.Contains(c.Name)))
                .Include(a => a.Categories)
                 .Include(a => a.User) // Include the User object
                .Include(a => a.Tags)  // Include Tags
                .Include(a => a.Images)
                .OrderByDescending(a => a.DateStamp) // Sort latest first
                .ToListAsync();

            // Fetch user's comments + article titles
            var userComments = await _dbContext.Comments
                .Where(c => c.UserId == userId)
                .Include(c => c.Article) // Include Article to get the title
                .OrderByDescending(c => c.DateStamp) // Sort latest first
                .ToListAsync();

            // Fetch user's liked comments (only comments linked to articles)
            var userCommentsLikes = await _dbContext.CommentLikes
                .Where(l => l.UserId == userId)
                .Include(l => l.Comment) // Include the Comment entity
                    .ThenInclude(c => c.Article) // Include Article for each Comment
                .Select(l => l.Comment) // Ensure we return Comment objects
                 .OrderByDescending(c => c.DateStamp)
                .ToListAsync();

            // Fetch user's subscriptions
            var subscriptions = await _dbContext.Subscriptions
                .Where(s => s.UserId == userId)
                .Include(s => s.SubscriptionType)
                .OrderByDescending(s => s.Expires)
                .ToListAsync();

            // Fetch user's liked articles
            var userLikedArticles = await _dbContext.ArticleLikes
                .Where(l => l.UserId == userId)
                .Select(l => l.Article)
                 .OrderByDescending(a => a.DateStamp)
                .ToListAsync();


            var userCommentReplies = await _dbContext.CommentReplies
      .Where(r => r.UserId == userId)
      .Include(r => r.Comment) // Include the comment the reply is associated with
          .ThenInclude(c => c.Article) // Include the article
         .OrderByDescending(r => r.DateStamp) // Sort latest first

      .ToListAsync();

            var userLikedCommentReplies = await _dbContext.CommentReplyLikes
        .Where(crl => crl.UserId == userId)
        .Include(crl => crl.CommentReply) // Include the liked reply
            .ThenInclude(cr => cr.Comment) // Include the parent comment
                .ThenInclude(c => c.Article) // Include the article for context
        .Include(crl => crl.CommentReply.User) // Include the reply owner
        .Select(crl => crl.CommentReply) // Return only the CommentReply objects
        .OrderByDescending(cr => cr.DateStamp)
        .ToListAsync();


            // Fetch statistics
            ViewBag.CommentCounts = await _dbContext.Comments
                .GroupBy(c => c.ArticleId)
                .ToDictionaryAsync(g => g.Key, g => g.Count());

            ViewBag.LikeCounts = await _dbContext.ArticleLikes
                .GroupBy(l => l.ArticleId)
                .ToDictionaryAsync(g => g.Key, g => g.Count());
            // *** Add view count statistics ***
            ViewBag.ViewCounts = await _dbContext.Articles
                .ToDictionaryAsync(a => a.Id, a => a.Views);

            var userArticleLikes = await _dbContext.ArticleLikes
      .Where(l => l.UserId == userId)
      .ToDictionaryAsync(l => l.ArticleId, l => true);

            ViewBag.IsLiked = userArticleLikes;


            Console.WriteLine($"✅ Loaded {userComments.Count} comments, {userCommentsLikes.Count} liked comments, {subscriptions.Count} subscriptions, {userLikedArticles.Count} liked articles, {exclusiveArticles.Count} exclusive articles for user {user.Id}");

            var viewModel = new MyPageViewModel
            {
                UserId = userId,
                UserName = user.UserName,
                FirstName = user.FirstName ?? "N/A",
                LastName = user.LastName ?? "N/A",
                PhoneNumber = user.PhoneNumber ?? "N/A",
                Email = user.Email ?? "N/A",
                City = user.City ?? "N/A",
                Country = user.Country ?? "N/A",
                UserComments = userComments,
                UserCommentsLikes = userCommentsLikes,
                Subscriptions = subscriptions,
                UserLikedArticles = userLikedArticles,
                ExclusiveArticles = exclusiveArticles,
                UserCommentReplies = userCommentReplies,
                UserLikedCommentReplies = userLikedCommentReplies
            };


            return View(viewModel);
        }




        [HttpPost("/User/ToggleArticleLike")]
        public async Task<IActionResult> ToggleArticleLike([FromBody] LikeDto likeDto)
        {
            var userId = _userManager.GetUserId(User);  // Get the current user's ID from the session

            if (string.IsNullOrEmpty(userId))  // Check if the user is logged in
            {
                return Unauthorized(new { success = false, message = "User not logged in" });
            }

            var existingLike = await _dbContext.ArticleLikes
                .FirstOrDefaultAsync(l => l.ArticleId == likeDto.ArticleId && l.UserId == userId);

            if (existingLike == null)
            {
                // If the user hasn't liked the article yet, add the like
                _dbContext.ArticleLikes.Add(new ArticleLike { ArticleId = likeDto.ArticleId, UserId = userId });
            }
            else
            {
                // If the user has already liked the article, remove the like
                _dbContext.ArticleLikes.Remove(existingLike);
            }

            await _dbContext.SaveChangesAsync();

            // Always increment views
            var article = await _dbContext.Articles.FindAsync(likeDto.ArticleId);
            if (article != null)
            {
                await TryIncrementArticleView(article.Id);
            }

            return Ok(new { success = true, message = "Article like status updated" });
        }

        // Add Comment
        [HttpPost("/User/AddComment")]
        public async Task<IActionResult> AddComment([FromBody] CommentDto commentDto)
        {
            var userId = _userManager.GetUserId(User);

            if (string.IsNullOrEmpty(commentDto.Content))
            {
                return BadRequest(new { success = false, message = "Comment content cannot be empty" });
            }

            var newComment = new Comment
            {
                ArticleId = commentDto.ArticleId,
                UserId = commentDto.UserId,
                Content = commentDto.Content,
                DateStamp = DateTime.Now
            };

            _dbContext.Comments.Add(newComment);
            await _dbContext.SaveChangesAsync();

            // Always increment views
            var article = await _dbContext.Articles.FindAsync(commentDto.ArticleId);
            if (article != null)
            {
                await TryIncrementArticleView(article.Id);
            }

            return Ok(new { success = true, commentId = newComment.Id, message = "Comment added successfully" });
        }
        // **2. Like a Comment**
        [HttpPost("/User/ToggleCommentLike")]
        public async Task<IActionResult> ToggleCommentLike([FromBody] CommentLikeDto commentLikeDto)
        {
            var userId = _userManager.GetUserId(User);

            var existingLike = await _dbContext.CommentLikes
                .FirstOrDefaultAsync(l => l.CommentId == commentLikeDto.CommentId && l.UserId == userId);

            // Find the comment to get its ArticleId
            var comment = await _dbContext.Comments.FindAsync(commentLikeDto.CommentId);
            if (comment == null)
            {
                return NotFound(new { success = false, message = "Comment not found" });
            }
            var articleId = comment.ArticleId;

            if (existingLike == null)
            {
                // Add a new like if it doesn't exist
                _dbContext.CommentLikes.Add(new CommentLike { CommentId = commentLikeDto.CommentId, UserId = userId });
            }
            else
            {
                // Remove the like if it exists
                _dbContext.CommentLikes.Remove(existingLike);
            }

            await _dbContext.SaveChangesAsync();

            // Always increment views
            var article = await _dbContext.Articles.FindAsync(articleId);
            if (article != null)
            {
                await TryIncrementArticleView(article.Id);
            }

            return Ok(new { success = true, message = "Comment like status updated" });
        }

        // Endpoint to fetch like counts, comment counts, and reply counts
        [HttpGet("/User/GetLikeCounts")]
        // Fetch like counts for both comments and replies
        public async Task<IActionResult> GetLikeCounts()
        {
            // Fetch comments with their like counts
            var commentsWithLikeCounts = await _dbContext.Comments
                .Select(c => new
                {
                    CommentId = c.Id,
                    CommentLikes = c.CommentLikes,  // Like count for the comment
                                             // Fetching like counts for replies of each comment
                    ReplyLikes = c.Replies.Select(r => r.CommentReplyLikes).ToList()
                })
                .ToListAsync();

            // Output the result (this could be logging or returning data to the client)
            foreach (var comment in commentsWithLikeCounts)
            {
                Console.WriteLine($"Comment ID: {comment.CommentId}, Likes: {comment.CommentLikes}");

                // Output reply likes
                foreach (var replyLike in comment.ReplyLikes)
                {
                    Console.WriteLine($"  Reply Like Count: {replyLike}");
                }
            }

            // Return as a response if needed (as JSON)
            return Ok(commentsWithLikeCounts);
        }



        // Get Article Likes
        [HttpGet("/User/GetArticleLikes")]
        public async Task<IActionResult> GetArticleLikes(int articleId)
        {
            int likeCount = await _dbContext.ArticleLikes
                .Where(l => l.ArticleId == articleId)
                .CountAsync();

            return Ok(new { likes = likeCount });
        }

        // **5. Show Replies for a Comment**

        [HttpPost("/User/AddReply")]
        public async Task<IActionResult> AddReply([FromBody] ReplyDto replyDto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Unauthorized();

            // Retrieve the user entity with tracking
            var user = await _dbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) return NotFound("User not found");

            if (string.IsNullOrEmpty(replyDto.Content))
            {
                return BadRequest(new { success = false, message = "Reply content cannot be empty" });
            }

            var newReply = new CommentReply
            {
                CommentId = replyDto.CommentId,
                UserId = userId,
                Content = replyDto.Content,
                DateStamp = DateTime.UtcNow
            };

            _dbContext.CommentReplies.Add(newReply);
            await _dbContext.SaveChangesAsync();

            // ✅ Reload the reply with User data
            var savedReply = await _dbContext.CommentReplies
                .Include(r => r.User)  // Ensure User is loaded
                .FirstOrDefaultAsync(r => r.Id == newReply.Id);

            if (savedReply == null)
            {
                return BadRequest(new { success = false, message = "Failed to save reply" });
            }

            return Ok(new
            {
                success = true,
                replyId = savedReply.Id,
                userName = savedReply.User?.UserName ?? "Anonymous", // Ensure UserName is returned
                message = "Reply added successfully"
            });
        }


        [HttpPost("User/LikeReply")]
        public async Task<IActionResult> LikeReply(int replyId)
        {
            var userId = _userManager.GetUserId(User);
            var existingLike = await _dbContext.CommentReplyLikes
                .FirstOrDefaultAsync(l => l.CommentReplyId == replyId && l.UserId == userId);

            if (existingLike == null)
            {
                _dbContext.CommentReplyLikes.Add(new CommentReplyLike { CommentReplyId = replyId, UserId = userId });
            }
            else
            {
                _dbContext.CommentReplyLikes.Remove(existingLike);
            }

            await _dbContext.SaveChangesAsync();

            // Fetch the reply to get the associated comment and article
            var reply = await _dbContext.CommentReplies.FindAsync(replyId);
            if (reply == null)
            {
                return NotFound(new { success = false, message = "Reply not found" });
            }

            var comment = await _dbContext.Comments.FindAsync(reply.CommentId);
            if (comment == null)
            {
                return NotFound(new { success = false, message = "Comment not found" });
            }
            var articleId = comment.ArticleId;

            // Always increment views
            var article = await _dbContext.Articles.FindAsync(articleId);
            if (article != null)
            {
                await TryIncrementArticleView(article.Id);
            }

            return Ok(new { success = true, message = "Reply like status updated" });
        }
        [HttpPost("User/UpdateReply")]
        public async Task<IActionResult> UpdateReply(int replyId, string newContent)
        {
            var userId = _userManager.GetUserId(User);
            var reply = await _dbContext.CommentReplies.FindAsync(replyId);

            if (reply == null)
            {
                return NotFound(new { success = false, message = "Reply not found" });
            }

            if (reply.UserId != userId)
            {
                return Forbid("You are not allowed to update this reply");
            }

            reply.Content = newContent;
            await _dbContext.SaveChangesAsync();

            // Fetch the comment to get the associated article
            var comment = await _dbContext.Comments.FindAsync(reply.CommentId);
            if (comment == null)
            {
                return NotFound(new { success = false, message = "Comment not found" });
            }
            var articleId = comment.ArticleId;

            // Always increment views
            var article = await _dbContext.Articles.FindAsync(articleId);
            if (article != null)
            {
                await TryIncrementArticleView(article.Id);
            }

            return Ok(new { success = true, message = "Reply updated successfully" });
        }


        // **9. Delete a Reply**
        [HttpPost("User/DeleteReply")]
        public async Task<IActionResult> DeleteReply(int replyId, string? source)
        {
            var userId = _userManager.GetUserId(User);
            var reply = await _dbContext.CommentReplies.FindAsync(replyId);

            if (reply == null)
            {
                return NotFound("Reply not found.");
            }

            if (reply.UserId != userId)
            {
                return Forbid("You are not allowed to delete this reply.");
            }

            _dbContext.CommentReplies.Remove(reply);
            await _dbContext.SaveChangesAsync();

            var comment = await _dbContext.Comments.FirstOrDefaultAsync(c => c.Id == reply.CommentId);
            if (comment == null)
            {
                return NotFound("Comment not found.");
            }
            var articleId = comment.ArticleId;
            var commentId = comment.Id;

            if (!string.IsNullOrEmpty(source) && source == "GetAllArticles")
            {
                var article = await _dbContext.Articles.FindAsync(articleId);
                if (article != null)
                {
                    await TryIncrementArticleView(article.Id);
                }
            }

            // Capture the referer URL
            var referer = Request.Headers["Referer"].ToString();

            // If the referer contains "/ViewArticle", redirect to the ViewArticle page with the articleId
            if (referer.Contains("/ViewArticle", StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction("ViewArticle", "Article", new { id = articleId, commentId = commentId, replyId = replyId });

            }
            else
            {
                // Otherwise, redirect back to the referer
                return Redirect(referer);
            }

        }


        private async Task<bool> TryIncrementArticleView(int articleId)
        {
            // Retrieve the session-stored viewed articles
            var viewedArticlesJson = HttpContext.Session.GetString("viewedArticles");
            var viewedArticles = string.IsNullOrEmpty(viewedArticlesJson)
                ? new HashSet<int>()  // Use HashSet instead of Dictionary, since we only care about article IDs
                : JsonConvert.DeserializeObject<HashSet<int>>(viewedArticlesJson) ?? new HashSet<int>();

            // If the article has already been viewed in the current session, return false
            if (viewedArticles.Contains(articleId))
            {
                return false; // Article was already viewed, so we don't increment
            }

            // Fetch the article and increment view count in the database
            var article = await _dbContext.Articles.FindAsync(articleId);
            if (article == null)
            {
                return false; // Article not found, do nothing
            }

            // Increment the view count in the database
            article.Views += 1;
            await _dbContext.SaveChangesAsync();

            // Add the article ID to the session (mark it as viewed)
            viewedArticles.Add(articleId);

            // Store the updated viewed articles in the session
            HttpContext.Session.SetString("viewedArticles", JsonConvert.SerializeObject(viewedArticles));

            return true; // Successfully incremented the view count
        }

        [HttpGet("Edit")]
        public async Task<IActionResult> Edit()
        {
            Console.WriteLine("Edit GET method triggered");

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                Console.WriteLine("User not found in GET method");
                return NotFound();
            }

            Console.WriteLine($"User found: {user.Id}");

            var model = new MyPageViewModel
            {
                UserId = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                PhoneNumber = user.PhoneNumber,
                Email = user.Email,
                City = user.City,
                Country = user.Country
            };

            return View(model);
        }

        [HttpPost("Edit")]
        public async Task<IActionResult> Edit(MyPageViewModel model)
        {
            Console.WriteLine("Edit POST method triggered");

            if (!ModelState.IsValid)
            {
                Console.WriteLine("Model state is invalid");
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    Console.WriteLine($"Model error: {error.ErrorMessage}");
                }
                return View(model);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                Console.WriteLine("User not found in POST method");
                return NotFound();
            }

            Console.WriteLine($"Updating user: {user.Id}");
            // Update only the necessary properties
            user.FirstName = model.FirstName;
            user.LastName = model.LastName;
            user.PhoneNumber = model.PhoneNumber;
            user.Email = model.Email;
            user.City = model.City;
            user.Country = model.Country;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                Console.WriteLine("User update failed");
                foreach (var error in result.Errors)
                {
                    Console.WriteLine($"Update error: {error.Description}");
                    ModelState.AddModelError("", error.Description);
                }
                return View(model);
            }

            Console.WriteLine("User updated successfully");
            await _signInManager.RefreshSignInAsync(user);
            return RedirectToAction("MyPage");
        }
        // Get Comments for a specific Article
        [HttpGet("/User/GetComments")]
        public async Task<IActionResult> GetComments(int articleId)
        {
            var comments = await _dbContext.Comments
                .Where(c => c.ArticleId == articleId)
                .OrderByDescending(c => c.DateStamp)
                .Select(c => new
                {
                    c.Id,
                    c.Content,
                    c.DateStamp,
                    UserName = c.User.UserName
                })
                .ToListAsync();

            if (!comments.Any())
            {
                return Ok(new { message = "No comments found." });
            }

            return Ok(comments);
        }
        [HttpGet("/User/GetReplies")]
        public async Task<IActionResult> GetReplies(int commentId)
        {
            var replies = await _dbContext.CommentReplies
                .Where(r => r.CommentId == commentId)
                .Include(r => r.User)  // ✅ Ensure User entity is loaded
                .OrderBy(r => r.DateStamp)
                .Select(r => new
                {
                    Id = r.Id,
                    Content = r.Content,
                    DateStamp = r.DateStamp,
                    UserName = r.User != null ? r.User.UserName : "Anonymous" // ✅ Ensure username is included
                })
                .ToListAsync();

            return Ok(replies);
        }

        public class ReplyDTO
        {
            public int Id { get; set; }
            public string Content { get; set; }
        }

        // DTO for liking a reply
        public class LikeReplyDTO
        {
            public int ReplyId { get; set; }
        }
        public class CommentDto
        {
            public int ArticleId { get; set; }
            public int CommentId { get; set; }
            public string UserId { get; set; }
            public string Content { get; set; }
        }

        // DTO for toggling likes
        public class LikeDto
        {
            public int ArticleId { get; set; }
            public string UserId { get; set; }
        }

        public class CommentLikeDto
        {
            public int CommentId { get; set; }
            public string UserId { get; set; }
        }

        // DTO for replying to a comment
        public class ReplyDto
        {
            public int CommentId { get; set; }
            public string Content { get; set; }

       

        }
    }
}