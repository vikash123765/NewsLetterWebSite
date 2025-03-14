using NewsLetterBanan.Models.ViewModels;

namespace NewsLetterBanan.Services.Interfaces
{
    public interface IJournalistService
    {
        void CreateArticleAndSave(CreateArticleViewModel model, string userId);
        Task<CreateArticleViewModel> GetArticleForEditAsync(int id);
        Task<bool> UpdateArticleAsync(int id, CreateArticleViewModel viewModel);
    }
}
