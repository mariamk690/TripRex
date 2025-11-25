using CoreTripRex.Models.RegisterSignInVM;
using CoreTripRex.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace CoreTripRex.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;

        public AccountController(UserManager<AppUser> userManager,
                                 SignInManager<AppUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        // LOGIN GET
        [HttpGet]
        public IActionResult Login()
        {
            var vm = new RegisterSignInVM
            {
                LoginEmail = Request.Cookies["SavedLoginEmail"] ?? string.Empty
            };
            return View("RegisterSignIn", vm);
        }

        // LOGIN POST
        [HttpPost]
        public async Task<IActionResult> Login(RegisterSignInVM model)
        {
            if (!ModelState.IsValid)
            {
                model.LoginError = "Invalid input.";
                return View("RegisterSignIn", model);
            }

            // ----- REMEMBER EMAIL COOKIE -----
            if (Request.Form["RememberEmail"] == "on")
            {
                Response.Cookies.Append("SavedLoginEmail", model.LoginEmail, new CookieOptions
                {
                    Expires = DateTimeOffset.UtcNow.AddDays(30)
                });
            }
            else
            {
                Response.Cookies.Delete("SavedLoginEmail");
            }
            // ----------------------------------

            // Find user BY EMAIL
            var user = await _userManager.FindByEmailAsync(model.LoginEmail);
            if (user == null)
            {
                model.LoginError = "Invalid email or password.";
                return View("RegisterSignIn", model);
            }

            // Verify password
            var pwCheck = await _signInManager.CheckPasswordSignInAsync(user, model.LoginPassword, false);
            if (!pwCheck.Succeeded)
            {
                model.LoginError = "Invalid email or password.";
                return View("RegisterSignIn", model);
            }

            // Actually sign in user
            await _signInManager.SignInAsync(user, false);

            return RedirectToAction("Index", "Dashboard");
        }


        // REGISTER GET
        [HttpGet]
        public IActionResult Register()
        {
            return View("RegisterSignIn", new RegisterSignInVM { ShowRegister = true });
        }
        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            Response.Cookies.Delete("SavedLoginEmail");
            return RedirectToAction("Index", "Dashboard");
        }

        // REGISTER POST
        [HttpPost]
        public async Task<IActionResult> Register(RegisterSignInVM model)
        {
            model.ShowRegister = true;

            if (!ModelState.IsValid)
            {
                model.RegisterError = "Invalid form input.";
                return View("RegisterSignIn", model);
            }

            if (model.Password != model.RepeatPassword)
            {
                model.RegisterError = "Passwords do not match.";
                return View("RegisterSignIn", model);
            }

            var user = new AppUser
            {
                UserName = model.Email,
                Email = model.Email,
                PhoneNumber = model.Phone,
                FullName = model.FullName
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                await _signInManager.SignInAsync(user, false);
                return RedirectToAction("Index", "Dashboard");
            }

            model.RegisterError = string.Join("<br>", result.Errors.Select(e => e.Description));
            return View("RegisterSignIn", model);
        }
    }
}
