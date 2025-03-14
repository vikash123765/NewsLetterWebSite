using Microsoft.AspNetCore.Mvc;
using NewsLetterBanan.Data;
using NewsLetterBanan.Models.ViewModels;
using static NewsLetterBanan.Controllers.AdminController;

namespace NewsLetterBanan.Services.Interfaces
{
    public interface IAdminService
    {
        void CreateArticleAndSave(CreateArticleViewModel model, string userId);
        Task<CreateArticleViewModel> GetArticleForEditAsync(int id);
        Task<bool> UpdateArticleAsync(int id, CreateArticleViewModel viewModel);

       


    }


}