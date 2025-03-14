using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NewsLetterBanan.Data;

namespace NewsLetterBanan.Controllers
{
    public class AccountController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;


        public AccountController(UserManager<IdentityUser> userManager, SignInManager<IdentityUser> signInManager, RoleManager<IdentityRole> roleManager)

        {

            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;

        }

   


        // Example: Confirming User's Email

        public async Task<IActionResult> ConfirmEmail(string userId, string token)

        {

            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)

            {

                return NotFound("User not found.");

            }


            var result = await _userManager.ConfirmEmailAsync(user, token);

            if (result.Succeeded)

            {

                return Ok("Email confirmed successfully!");

            }


            return BadRequest("Failed to confirm email.");

        }
        [HttpPost]

        public async Task<IActionResult> Login(string userName, string password)

        {

            var result = await _signInManager.PasswordSignInAsync(userName, password, isPersistent: false, lockoutOnFailure: true);


            if (result.Succeeded)

            {

                return RedirectToAction("Index", "Home");

            }


            if (result.IsLockedOut)

            {

                return BadRequest("Account is locked out.");

            }


            return BadRequest("Invalid login attempt.");

        }


        [HttpPost]

        public async Task<IActionResult> Logout()

        {

            await _signInManager.SignOutAsync();

            return RedirectToAction("Index", "Home");

        }
        [HttpGet]

        public IActionResult ExternalLogin(string provider)

        {

            var redirectUrl = Url.Action(nameof(ExternalLoginCallback), "Account");

            var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);

            return Challenge(properties, provider);

        }


        // Handle the callback from the external provider

        [HttpGet]

        public async Task<IActionResult> ExternalLoginCallback()

        {

            var info = await _signInManager.GetExternalLoginInfoAsync();

            if (info == null)

            {

                return RedirectToAction("Login");

            }
            // Sign in the user (or register them)

            var result = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: false);


            if (result.Succeeded)

            {

                return RedirectToAction("Index", "Home");

            }


            // If not registered, redirect to a registration page

            return RedirectToAction("RegisterExternal");

        }
    }
}