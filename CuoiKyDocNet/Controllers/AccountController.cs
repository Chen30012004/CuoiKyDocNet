using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using CuoiKyDocNet.Data;
using CuoiKyDocNet.Models;
using CuoiKyDocNet.Services;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Logging;

namespace CuoiKyDocNet.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly PodcastContext _context;
        private readonly EmailService _emailService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<IdentityRole> roleManager,
            PodcastContext context,
            EmailService emailService,
            IConfiguration configuration,
            ILogger<AccountController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _context = context;
            _emailService = emailService;
            _configuration = configuration;
            _logger = logger;
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string returnUrl = null)
        {
            var model = new LoginViewModel
            {
                ReturnUrl = returnUrl ?? Url.Action("Index", "Home")
            };
            return View(model);
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user == null || !user.EmailConfirmed)
                {
                    _logger.LogWarning("Login failed for {Email}: Invalid email or not verified.", model.Email);
                    TempData["ErrorMessage"] = "Invalid email or email not verified.";
                    return View(model);
                }

                var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, false);
                if (result.Succeeded)
                {
                    _logger.LogInformation("User {Email} logged in successfully.", model.Email);

                    var authClaims = new[]
                    {
                        new Claim(ClaimTypes.Name, user.Email),
                        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
                    };
                    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
                    var token = new JwtSecurityToken(
                        issuer: _configuration["Jwt:Issuer"],
                        audience: _configuration["Jwt:Audience"],
                        expires: DateTime.Now.AddHours(3),
                        claims: authClaims,
                        signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
                    );
                    var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
                    Response.Cookies.Append("JWT", tokenString, new CookieOptions { HttpOnly = true });

                    return RedirectToLocal(model.ReturnUrl ?? Url.Action("Index", "Home"));
                }
                _logger.LogWarning("Login failed for {Email}: Invalid password.", model.Email);
                TempData["ErrorMessage"] = "Invalid login attempt.";
                return View(model);
            }
            _logger.LogWarning("Invalid model state for login attempt with email {Email}.", model.Email);
            return View(model);
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult SignUp()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> SignUp(SignUpViewModel model)
        {
            if (ModelState.IsValid)
            {
                var existingUser = await _userManager.FindByEmailAsync(model.Email);
                if (existingUser != null)
                {
                    _logger.LogWarning("SignUp failed: Email {Email} already registered.", model.Email);
                    ModelState.AddModelError(string.Empty, "This email is already registered.");
                    return View(model);
                }

                // Tạo vai trò "User" và "Admin" nếu chưa tồn tại
                if (!await _roleManager.RoleExistsAsync("User"))
                {
                    await _roleManager.CreateAsync(new IdentityRole("User"));
                    _logger.LogInformation("Role 'User' created successfully.");
                }
                if (!await _roleManager.RoleExistsAsync("Admin"))
                {
                    await _roleManager.CreateAsync(new IdentityRole("Admin"));
                    _logger.LogInformation("Role 'Admin' created successfully.");
                }

                var user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    FullName = model.FullName,
                    ReceiveEmailNotifications = model.ReceiveEmailNotifications,
                    EmailConfirmed = false,
                    VerificationCode = new Random().Next(100000, 999999).ToString()
                };

                var result = await _userManager.CreateAsync(user, model.Password);
                if (result.Succeeded)
                {
                    // Gán vai trò "User" cho người dùng thông thường
                    await _userManager.AddToRoleAsync(user, "User");

                    try
                    {
                        var emailBody = $"Your verification code is: <strong>{user.VerificationCode}</strong>";
                        await _emailService.SendEmailAsync(user.Email, "Verify Your Email", emailBody);
                        _logger.LogInformation("Verification email sent to {Email}.", user.Email);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to send verification email to {Email}.", user.Email);
                        ModelState.AddModelError(string.Empty, "Failed to send verification email. Please try again later.");
                        return View(model);
                    }

                    _logger.LogInformation("User {Email} registered successfully.", user.Email);
                    TempData["SuccessMessage"] = "Account created successfully. Please verify your email.";
                    return RedirectToAction("VerifyEmail", new { email = user.Email });
                }

                foreach (var error in result.Errors)
                {
                    _logger.LogError("SignUp error for {Email}: {Error}", model.Email, error.Description);
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
            else
            {
                _logger.LogWarning("Invalid model state for SignUp attempt with email {Email}.", model.Email);
            }
            return View(model);
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult VerifyEmail(string email)
        {
            var model = new VerifyEmailViewModel { Email = email };
            return View(model);
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> VerifyEmail(VerifyEmailViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user == null)
                {
                    _logger.LogWarning("Email verification failed: Email {Email} not found.", model.Email);
                    ModelState.AddModelError(string.Empty, "Email not found.");
                    return View(model);
                }

                if (user.VerificationCode == model.Code)
                {
                    user.EmailConfirmed = true;
                    user.VerificationCode = "";
                    var result = await _userManager.UpdateAsync(user);
                    if (result.Succeeded)
                    {
                        _logger.LogInformation("Email {Email} verified successfully.", model.Email);
                        TempData["SuccessMessage"] = "Email verified successfully. You can now log in.";
                        return RedirectToAction("Login");
                    }

                    foreach (var error in result.Errors)
                    {
                        _logger.LogError("Update user error for {Email}: {Error}", model.Email, error.Description);
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                }
                else
                {
                    _logger.LogWarning("Email verification failed for {Email}: Invalid code.", model.Email);
                    ModelState.AddModelError(string.Empty, "Invalid verification code.");
                }
            }
            return View(model);
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                _logger.LogWarning("Profile access failed: User not found.");
                return RedirectToAction("Login");
            }

            var model = new ProfileViewModel
            {
                Email = user.Email,
                FullName = user.FullName,
                ReceiveEmailNotifications = user.ReceiveEmailNotifications
            };

            _logger.LogInformation("User {Email} accessed their profile.", user.Email);
            return View(model);
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> EditProfile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                _logger.LogWarning("EditProfile access failed: User not found.");
                return RedirectToAction("Login");
            }

            var model = new EditProfileViewModel
            {
                Email = user.Email,
                FullName = user.FullName,
                ReceiveEmailNotifications = user.ReceiveEmailNotifications
            };

            _logger.LogInformation("User {Email} accessed EditProfile.", user.Email);
            return View(model);
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> EditProfile(EditProfileViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    _logger.LogWarning("EditProfile update failed: User not found.");
                    return RedirectToAction("Login");
                }

                user.FullName = model.FullName;
                user.ReceiveEmailNotifications = model.ReceiveEmailNotifications;

                var result = await _userManager.UpdateAsync(user);
                if (result.Succeeded)
                {
                    _logger.LogInformation("User {Email} profile updated successfully.", user.Email);
                    TempData["SuccessMessage"] = "Profile updated successfully.";
                    return RedirectToAction("Profile");
                }

                foreach (var error in result.Errors)
                {
                    _logger.LogError("EditProfile update error for {Email}: {Error}", user.Email, error.Description);
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
            return View(model);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            Response.Cookies.Delete("JWT");
            _logger.LogInformation("User logged out successfully.");
            return RedirectToAction("Index", "Home");
        }

        // Chỉ giữ một phương thức RedirectToLocal
        private IActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction("Index", "Home");
        }
    }
}