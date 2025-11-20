using CoreTripRex.Models.RegisterSignInVM;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace CoreTripRex.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;

        public AccountController(UserManager<IdentityUser> userManager,
                                 SignInManager<IdentityUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        // LOGIN GET
        [HttpGet]
        public IActionResult Login()
        {
            return View("RegisterSignIn", new RegisterSignInVM());
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

            var result = await _signInManager.PasswordSignInAsync(
                model.LoginEmail,
                model.LoginPassword,
                false,
                false
            );

            if (result.Succeeded)
                return RedirectToAction("Index", "Dashboard");

            model.LoginError = "Invalid email or password.";
            return View("RegisterSignIn", model);
        }

        // REGISTER GET
        [HttpGet]
        public IActionResult Register()
        {
            return View("RegisterSignIn", new RegisterSignInVM { ShowRegister = true });
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

            var user = new IdentityUser
            {
                UserName = model.Email,
                Email = model.Email,
                PhoneNumber = model.Phone
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
