using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NewsLetterBanan.Data;
using NewsLetterBanan.Models.ViewModels;
using NewsLetterBanan.Services.Interfaces;
using static NewsLetterBanan.Controllers.AdminController;

namespace NewsLetterBanan.Services
{
    public class AdminService : IAdminService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AdminService(ApplicationDbContext context, UserManager<User> userManager, RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

 
            public void CreateArticleAndSave(CreateArticleViewModel model, string userId)
            {
                // Create the Article
                var article = new Article
                {
                    Headline = model.Headline,
                    Content = model.Content,
                    ContentSummary = model.ContentSummary,
                    DateStamp = DateTime.Now,
                    SourceURL = model.SourceURL,
                    IsArchived = model.IsArchived,
                    IsApproved=model.IsApproved,
                    CommentsOnOff = model.CommentsOnOff,
                    UserId = userId,
                    IsEditorsChoice = model.IsEditorsChoice,
                    Exclusive = model.Exclusive
                };

                // Handle Tag logic
                if (!string.IsNullOrEmpty(model.TagNames))
                {
                    var tagNames = model.TagNames.Split(',').Select(t => t.Trim()).ToList();
                    var tagDescriptions = model.TagDescriptions.Split(',').Select(d => d.Trim()).ToList();

                    for (int i = 0; i < tagNames.Count; i++)
                    {
                        var existingTag = _context.Tags.FirstOrDefault(t => t.TagName == tagNames[i]);
                        if (existingTag != null)
                        {
                            // Use the existing tag
                            article.Tags.Add(existingTag);
                        }
                        else
                        {
                            // Create a new tag
                            var newTag = new Tag
                            {
                                TagName = tagNames[i],
                                TagDescription = i < tagDescriptions.Count ? tagDescriptions[i] : "" // Default to empty if descriptions are fewer than tags
                            };
                            _context.Tags.Add(newTag);
                            article.Tags.Add(newTag);
                        }
                    }
                }

                // Handle Category logic
                if (!string.IsNullOrEmpty(model.CategoryNames))
                {
                    var categoryNames = model.CategoryNames.Split(',').Select(c => c.Trim()).ToList();
                    var categoryDescriptions = model.CategoryDescriptions.Split(',').Select(d => d.Trim()).ToList();

                    for (int i = 0; i < categoryNames.Count; i++)
                    {
                        var existingCategory = _context.Categories.FirstOrDefault(c => c.Name == categoryNames[i]);
                        if (existingCategory != null)
                        {
                            // Use the existing category
                            article.Categories.Add(existingCategory);
                        }
                        else
                        {
                            // Create a new category
                            var newCategory = new Category
                            {
                                Name = categoryNames[i],
                                Description = i < categoryDescriptions.Count ? categoryDescriptions[i] : "" // Default to empty if descriptions are fewer than categories
                            };
                            _context.Categories.Add(newCategory);
                            article.Categories.Add(newCategory);
                        }
                    }
                }

                // Handle Image logic
                if (!string.IsNullOrEmpty(model.Titles))
                {
                    var imageTitles = model.Titles.Split(',').Select(c => c.Trim()).ToList();
                    var imageDescriptions = model.ImgDescriptions.Split(',').Select(d => d.Trim()).ToList();
                    var imgSourceURLs = model.ImgSourceURLs.Split(',').Select(d => d.Trim()).ToList();
                    var takenBys = model.TakenBys.Split(',').Select(d => d.Trim()).ToList();
                    var licenses = model.Licenses.Split(',').Select(d => d.Trim()).ToList();

                    for (int i = 0; i < imageTitles.Count; i++)
                    {
                        var existingImage = _context.Images.FirstOrDefault(c => c.Title == imageTitles[i]);
                        if (existingImage != null)
                        {
                            // Use the existing image
                            article.Images.Add(existingImage);
                        }
                        else
                        {
                            // Create a new image
                            var newImage = new Image
                            {
                                Title = imageTitles[i],
                                ImgDescription = i < imageDescriptions.Count ? imageDescriptions[i] : "",
                                ImgSourceURL = i < imgSourceURLs.Count ? imgSourceURLs[i] : "",
                                TakenBy = i < takenBys.Count ? takenBys[i] : "",
                                License = i < licenses.Count ? licenses[i] : "",
                                Article = article
                            };
                            _context.Images.Add(newImage);
                            article.Images.Add(newImage);
                        }
                    }
                }

                // Save the article to the database
                _context.Articles.Add(article);
                _context.SaveChanges();
            }

        // Method to get the article from the database and map it to the view model
        public async Task<CreateArticleViewModel> GetArticleForEditAsync(int id)
        {
            var article = await _context.Articles
                .Include(a => a.Categories)
                .Include(a => a.Tags)
                .Include(a => a.Images)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (article == null)
            {
                return null; // return null if article is not found
            }

            // Mapping Article entity to CreateArticleViewModel
            var viewModel = new CreateArticleViewModel
            {
                Headline = article.Headline,
                Content = article.Content,
                ContentSummary = article.ContentSummary,
                SourceURL = article.SourceURL,
                IsArchived = article.IsArchived,
                IsApproved = article.IsApproved,
                CommentsOnOff = article.CommentsOnOff,

                IsEditorsChoice = article.IsEditorsChoice,
                // Map the Exclusive property
                Exclusive = article.Exclusive,

                // Convert categories and tags into comma-separated strings
                CategoryNames = string.Join(", ", article.Categories.Select(c => c.Name)),
                CategoryDescriptions = string.Join(", ", article.Categories.Select(c => c.Description)),

                TagNames = string.Join(", ", article.Tags.Select(t => t.TagName)),
                TagDescriptions = string.Join(", ", article.Tags.Select(t => t.TagDescription)),

                Titles = string.Join(", ", article.Images.Select(i => i.Title)),
                ImgDescriptions = string.Join(", ", article.Images.Select(i => i.ImgDescription)),
                ImgSourceURLs = string.Join(", ", article.Images.Select(i => i.ImgSourceURL)),
                TakenBys = string.Join(", ", article.Images.Select(i => i.TakenBy)),
                Licenses = string.Join(", ", article.Images.Select(i => i.License))
            };

            return viewModel;
        }

        // Method to update the article in the database
        public async Task<bool> UpdateArticleAsync(int id, CreateArticleViewModel viewModel)
        {
            var article = await _context.Articles
                .Include(a => a.Categories)
                .Include(a => a.Tags)
                .Include(a => a.Images)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (article == null)
            {
                return false;
            }

            // Update article properties (same logic as in the controller)
            article.Headline = viewModel.Headline;
            article.Content = viewModel.Content;
            article.ContentSummary = viewModel.ContentSummary;
            article.SourceURL = viewModel.SourceURL;
            article.IsArchived = viewModel.IsArchived;
            article.IsApproved = viewModel.IsApproved;
            article.CommentsOnOff = viewModel.CommentsOnOff;
            article.IsEditorsChoice = viewModel.IsEditorsChoice;
            article.Exclusive = viewModel.Exclusive;



            // Clear existing categories, tags, and images
            article.Categories.Clear();
            article.Tags.Clear();
            article.Images.Clear();

            // Handle Categories (same logic as controller)
            var categoryNames = viewModel.CategoryNames.Split(',').Select(c => c.Trim()).ToList();
            var categoryDescriptions = viewModel.CategoryDescriptions.Split(',').Select(d => d.Trim()).ToList();

            for (int i = 0; i < categoryNames.Count; i++)
            {
                var categoryName = categoryNames[i];
                var categoryDesc = i < categoryDescriptions.Count ? categoryDescriptions[i] : "";

                var existingCategory = await _context.Categories
                    .FirstOrDefaultAsync(c => c.Name == categoryName);

                if (existingCategory == null)
                {
                    existingCategory = new Category
                    {
                        Name = categoryName,
                        Description = categoryDesc
                    };
                    _context.Categories.Add(existingCategory);
                }

                article.Categories.Add(existingCategory);
            }

            // Handle Tags (same logic as controller)
            var tagNames = viewModel.TagNames.Split(',').Select(t => t.Trim()).ToList();
            var tagDescriptions = viewModel.TagDescriptions.Split(',').Select(d => d.Trim()).ToList();

            for (int i = 0; i < tagNames.Count; i++)
            {
                var tagName = tagNames[i];
                var tagDesc = i < tagDescriptions.Count ? tagDescriptions[i] : "";

                var existingTag = await _context.Tags
                    .FirstOrDefaultAsync(t => t.TagName == tagName);

                if (existingTag == null)
                {
                    existingTag = new Tag
                    {
                        TagName = tagName,
                        TagDescription = tagDesc
                    };
                    _context.Tags.Add(existingTag);
                }

                article.Tags.Add(existingTag);
            }

            // Handle Images (same logic as controller)
            var imageTitles = viewModel.Titles.Split(',').Select(i => i.Trim()).ToList();
            var imageDescriptions = viewModel.ImgDescriptions.Split(',').Select(d => d.Trim()).ToList();
            var imgSourceURLs = viewModel.ImgSourceURLs.Split(',').Select(d => d.Trim()).ToList();
            var takenBys = viewModel.TakenBys.Split(',').Select(d => d.Trim()).ToList();
            var licenses = viewModel.Licenses.Split(',').Select(d => d.Trim()).ToList();

            for (int i = 0; i < imageTitles.Count; i++)
            {
                var existingImage = await _context.Images
                    .FirstOrDefaultAsync(img => img.Title == imageTitles[i]);

                if (existingImage != null)
                {
                    article.Images.Add(existingImage);
                }
                else
                {
                    var newImage = new Image
                    {
                        Title = imageTitles[i],
                        ImgDescription = i < imageDescriptions.Count ? imageDescriptions[i] : "",
                        ImgSourceURL = i < imgSourceURLs.Count ? imgSourceURLs[i] : "",
                        TakenBy = i < takenBys.Count ? takenBys[i] : "",
                        License = i < licenses.Count ? licenses[i] : ""
                    };

                    _context.Images.Add(newImage);
                    article.Images.Add(newImage);
                }
            }

            // Save changes
            await _context.SaveChangesAsync();
            return true;
        }

  



    }
}
    

