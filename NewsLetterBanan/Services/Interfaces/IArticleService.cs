using NewsLetterBanan.Data;

namespace NewsLetterBanan.Services.Interfaces
{
    public interface IArticleService
    {
        Task<List<Article>> GetLatestArticlesAsync();
        Article GetArticleById(int id);

        Task AddArticleAsync(Article article);
        Task<Article> GetArticleByIdAsync(int id);
        Task DeleteArticleAsync(int id);
    }
}
