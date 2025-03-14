using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NewsLetterBanan.Data;
using NewsLetterBanan.Models.ViewModels;
using NewsLetterBanan.Services.Interfaces;

namespace NewsLetterBanan.Services
{
    public class UserService : IUserService
    {
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly UserManager<User> _userManager;

        private readonly ApplicationDbContext _context;



        public UserService(UserManager<User> userManager, RoleManager<IdentityRole> roleManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
        }

      



        public async Task AddUserAsync(User user)

        {
            // Check if the article is null before trying to add it to the database
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user), "User cannot be null");
            }

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

        }
       




    }
}
