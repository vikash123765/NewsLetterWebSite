using Microsoft.EntityFrameworkCore;
using NewsLetterBanan.Data;
using NewsLetterBanan.Services.Interfaces;

namespace NewsLetterBanan.Services
{
    public class ArticleService : IArticleService
    {
        private readonly ApplicationDbContext _context;
        public ArticleService(ApplicationDbContext context)
        {
            _context = context;
        }
        public async Task<List<Article>> GetLatestArticlesAsync()
        {
            return await _context.Articles
                .OrderByDescending(a => a.DateStamp)
                .Include(a => a.Images)
                .Take(10) // Get the latest 10 articles
                .ToListAsync();
        }

        public async Task AddArticleAsync(Article article)
        {
            // Check if the article is null before trying to add it to the database
            if (article == null)
            {
                throw new ArgumentNullException(nameof(article), "Article cannot be null");
            }

            // Add the article to the context
            _context.Articles.Add(article);
            await _context.SaveChangesAsync();
        }

        public async Task<Article> GetArticleByIdAsync(int id)
        {
            return await _context.Articles.FindAsync(id);
        }

        public async Task DeleteArticleAsync(int id)
        {
            var article = await _context.Articles.FindAsync(id);
            if (article != null)
            {
                _context.Articles.Remove(article);
                await _context.SaveChangesAsync();
            }
        }

        public Article GetArticleById(int id)
        {
            return _context.Articles
                .FirstOrDefault(a => a.Id == id); // Find the article by its ID
        }

    }

}