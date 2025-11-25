using CoreTripRex.Data;
using CoreTripRex.Models;
using CoreTripRex.Models.RegisterSignInVM;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CoreTripRex.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly ApplicationDbContext _context;

        public AccountController(UserManager<AppUser> userManager,
                                 SignInManager<AppUser> signInManager,
                                 ApplicationDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
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
        [HttpGet]
        public IActionResult ForgotUsername()
        {
            return View(new ForgotUsernameVM());
        }
        [HttpPost]
        public async Task<IActionResult> ForgotUsername(ForgotUsernameVM model)
        {
            if (!model.ShowQuestion)
            {
                var user = _userManager.Users
                    .FirstOrDefault(u => u.FullName == model.FullName && u.PhoneNumber == model.Phone);

                if (user == null)
                {
                    ModelState.AddModelError("", "No user found with that name and phone number.");
                    return View(model);
                }

                var questions = _context.UserSecurityQuestions
                    .Where(q => q.UserId == user.Id)
                    .ToList();

                if (!questions.Any())
                {
                    ModelState.AddModelError("", "No security questions found for this user.");
                    return View(model);
                }

                var random = new Random();
                var selected = questions[random.Next(questions.Count)];

                model.Question = selected.Question;
                model.ShowQuestion = true;

                HttpContext.Session.SetString("RecoverUserId", user.Id);

                return View(model);
            }
            else
            {
                var userId = HttpContext.Session.GetString("RecoverUserId");
                if (userId == null)
                {
                    ModelState.AddModelError("", "Session expired. Try again.");
                    return View(model);
                }

                var q = _context.UserSecurityQuestions
                    .FirstOrDefault(q => q.UserId == userId && q.Question == model.Question);

                if (q == null)
                {
                    ModelState.AddModelError("", "Security question not found.");
                    return View(model);
                }

                var user = await _userManager.FindByIdAsync(userId);

                var verify = _userManager.PasswordHasher.VerifyHashedPassword(
                    user,
                    q.AnswerHash,
                    model.Answer ?? string.Empty
                );

                if (verify == PasswordVerificationResult.Failed)
                {
                    ModelState.AddModelError("", "Incorrect answer.");
                    return View(model);
                }

                model.EmailResult = user.Email;
                model.ShowResult = true;

                return View(model);
            }
        }

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View(new ForgotPasswordVM());
        }
        [HttpPost]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordVM model)
        {
            if (!model.ShowQuestion)
            {
                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user == null)
                {
                    ModelState.AddModelError("", "No account found for that email.");
                    return View(model);
                }

                // Pull security questions
                var questions = _context.UserSecurityQuestions
                    .Where(q => q.UserId == user.Id)
                    .ToList();

                if (!questions.Any())
                {
                    ModelState.AddModelError("", "This account has no security questions.");
                    return View(model);
                }

                // Pick random question
                var random = new Random();
                var selected = questions[random.Next(questions.Count)];

                // Save question + state
                model.Question = selected.Question;
                model.ShowQuestion = true;

                // Store values in session for the next step
                HttpContext.Session.SetString("PWResetUserId", user.Id);
                HttpContext.Session.SetString("PWResetEmail", model.Email);
                HttpContext.Session.SetString("PWResetQuestion", selected.Question);

                return View(model);
            }

            else
            {
                var userId = HttpContext.Session.GetString("PWResetUserId");
                var email = HttpContext.Session.GetString("PWResetEmail");
                var question = HttpContext.Session.GetString("PWResetQuestion");

                if (userId == null || email == null || question == null)
                {
                    ModelState.AddModelError("", "Session expired. Please try again.");
                    return View(model);
                }

                var q = _context.UserSecurityQuestions
                    .FirstOrDefault(q => q.UserId == userId && q.Question == question);

                var user = await _userManager.FindByIdAsync(userId);

                var result = _userManager.PasswordHasher.VerifyHashedPassword(
                    user, q!.AnswerHash, model.Answer ?? "");

                if (result == PasswordVerificationResult.Failed)
                {
                    ModelState.AddModelError("", "Incorrect answer.");
                    return View(model);
                }

                var token = await _userManager.GeneratePasswordResetTokenAsync(user);

                var resetLink = Url.Action(
                    "ResetPassword",
                    "Account",
                    new { token, email },
                    Request.Scheme);
                //write email sending logic here to send the resetLink to user's email address
                Console.WriteLine("RESET LINK: " + resetLink);

                TempData["ResetLink"] = resetLink;

                return RedirectToAction("PasswordResetSent");
            }
        }

        [HttpGet]
        public IActionResult PasswordResetSent()
        {
            return View();
        }
        [HttpGet]
        public IActionResult ResetPassword(string token, string email)
        {
            return View(new ResetPasswordVM
            {
                Token = token,
                Email = email
            });
        }
        [HttpPost]
        public async Task<IActionResult> ResetPassword(ResetPasswordVM model)
        {
            if (model.NewPassword != model.ConfirmPassword)
            {
                model.ErrorMessage = "Passwords do not match.";
                return View(model);
            }

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                model.ErrorMessage = "User not found.";
                return View(model);
            }

            var result = await _userManager.ResetPasswordAsync(user, model.Token, model.NewPassword);

            if (!result.Succeeded)
            {
                model.ErrorMessage = string.Join("<br>", result.Errors.Select(e => e.Description));
                return View(model);
            }

            model.SuccessMessage = "Your password has been reset successfully!";
            return View(model);
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
                var questions = new List<UserSecurityQuestions>
        {
            new UserSecurityQuestions
            {
                UserId = user.Id,
                Question = model.SecurityQuestion1 ?? string.Empty,
                AnswerHash = _userManager.PasswordHasher.HashPassword(user, model.SecurityAnswer1 ?? string.Empty)
            },
            new UserSecurityQuestions
            {
                UserId = user.Id,
                Question = model.SecurityQuestion2 ?? string.Empty,
                AnswerHash = _userManager.PasswordHasher.HashPassword(user, model.SecurityAnswer2 ?? string.Empty)
            },
            new UserSecurityQuestions
            {
                UserId = user.Id,
                Question = model.SecurityQuestion3 ?? string.Empty,
                AnswerHash = _userManager.PasswordHasher.HashPassword(user, model.SecurityAnswer3 ?? string.Empty)
            }
        };

                _context.UserSecurityQuestions.AddRange(questions);
                await _context.SaveChangesAsync();

                var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);

                var confirmationLink = Url.Action(
                    "ConfirmEmail",
                    "Account",
                    new { userId = user.Id, token = token },
                    Request.Scheme);

                Console.WriteLine("EMAIL CONFIRMATION LINK: " + confirmationLink);
                TempData["ConfirmLink"] = confirmationLink;

                return RedirectToAction("EmailVerificationSent");
            }

            model.RegisterError = string.Join("<br>", result.Errors.Select(e => e.Description));
            return View("RegisterSignIn", model);
        }
        [HttpGet]
        public IActionResult EmailVerificationSent()
        {
            return View();
        }
        [HttpGet]
        public async Task<IActionResult> ConfirmEmail(string userId, string token)
        {
            if (userId == null || token == null)
                return BadRequest("Invalid confirmation link.");

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return NotFound("User not found.");

            var result = await _userManager.ConfirmEmailAsync(user, token);

            if (result.Succeeded)
            {
                TempData["Message"] = "Your email has been successfully confirmed.";
                return RedirectToAction("ConfirmedEmail");
            }

            return BadRequest("Email confirmation failed.");
        }
        [HttpGet]
        public IActionResult ConfirmedEmail()
        {
            return View();
        }

    }
}
