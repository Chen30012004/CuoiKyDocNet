using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using CuoiKyDocNet.Data;
using CuoiKyDocNet.Models;
using CuoiKyDocNet.Services;
using System.Threading.Tasks;

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
                    if (await _roleManager.RoleExistsAsync("User"))
                    {
                        await _userManager.AddToRoleAsync(user, "User");
                    }

                    // Gửi email xác minh
                    var emailBody = $"Your verification code is: <strong>{user.VerificationCode}</strong>";
                    await _emailService.SendEmailAsync(user.Email, "Verify Your Email", emailBody);
                    _logger.LogInformation("Verification email sent to {Email}.", user.Email);

                    TempData["SuccessMessage"] = "Account created successfully. Please verify your email.";
                    return RedirectToAction("VerifyEmail", new { email = user.Email });
                }

                foreach (var error in result.Errors)
                {
                    _logger.LogError("SignUp error for {Email}: {Error}", model.Email, error.Description);
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
            return View(model);
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl ?? Url.Action("Index", "Home");
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

                    // Tạo JWT token
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

                    return RedirectToLocal(returnUrl);
                }
                _logger.LogWarning("Login failed for {Email}: Invalid password.", model.Email);
                TempData["ErrorMessage"] = "Invalid login attempt.";
                return View(model);
            }
            _logger.LogWarning("Invalid model state for login attempt.");
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
                    user.VerificationCode = null;
                    await _userManager.UpdateAsync(user);
                    _logger.LogInformation("Email {Email} verified successfully.", model.Email);
                    TempData["SuccessMessage"] = "Email verified successfully. You can now log in.";
                    return RedirectToAction("Login");
                }
                _logger.LogWarning("Email verification failed for {Email}: Invalid code.", model.Email);
                ModelState.AddModelError(string.Empty, "Invalid verification code.");
            }
            return View(model);
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user == null || !(await _userManager.IsEmailConfirmedAsync(user)))
                {
                    _logger.LogInformation("Forgot password requested for {Email}. No action taken (email not found or not verified).", model.Email);
                    TempData["SuccessMessage"] = "If your email exists, a reset link has been sent.";
                    return RedirectToAction("ForgotPassword");
                }

                var code = await _userManager.GeneratePasswordResetTokenAsync(user);
                var callbackUrl = Url.Action("ResetPassword", "Account", new { userId = user.Id, code }, protocol: Request.Scheme);
                var emailBody = $"Please reset your password by clicking here: <a href='{callbackUrl}'>Reset Password</a>";
                await _emailService.SendEmailAsync(user.Email, "Reset Your Password", emailBody);
                _logger.LogInformation("Password reset email sent to {Email}.", model.Email);

                TempData["SuccessMessage"] = "A reset link has been sent to your email.";
                return RedirectToAction("ForgotPassword");
            }
            return View(model);
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResetPassword(string userId, string code)
        {
            if (userId == null || code == null)
            {
                return BadRequest("Invalid reset password request.");
            }
            var model = new ResetPasswordViewModel { UserId = userId, Code = code };
            return View(model);
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByIdAsync(model.UserId);
                if (user == null)
                {
                    _logger.LogWarning("Reset password failed: User ID {UserId} not found.", model.UserId);
                    TempData["SuccessMessage"] = "Password reset link is invalid or has expired.";
                    return RedirectToAction("Login");
                }

                var result = await _userManager.ResetPasswordAsync(user, model.Code, model.Password);
                if (result.Succeeded)
                {
                    _logger.LogInformation("Password reset successfully for user {Email}.", user.Email);
                    TempData["SuccessMessage"] = "Password reset successfully. You can now log in.";
                    return RedirectToAction("Login");
                }

                foreach (var error in result.Errors)
                {
                    _logger.LogError("Reset password error for {Email}: {Error}", user.Email, error.Description);
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
            return View(model);
        }

        [HttpGet]
        [Authorize]
        public IActionResult ChangePassword()
        {
            return View();
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    _logger.LogWarning("Change password failed: User not found.");
                    return RedirectToAction("Login");
                }

                var result = await _userManager.ChangePasswordAsync(user, model.OldPassword, model.NewPassword);
                if (result.Succeeded)
                {
                    await _signInManager.RefreshSignInAsync(user);
                    _logger.LogInformation("Password changed successfully for user {Email}.", user.Email);
                    TempData["SuccessMessage"] = "Password changed successfully.";
                    return RedirectToAction("Index", "Home");
                }

                foreach (var error in result.Errors)
                {
                    _logger.LogError("Change password error for {Email}: {Error}", user.Email, error.Description);
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
            return View(model);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            var userEmail = User.Identity.Name;
            await _signInManager.SignOutAsync();
            Response.Cookies.Delete("JWT");
            _logger.LogInformation("User {Email} logged out successfully.", userEmail);
            return RedirectToAction("Index", "Home");
        }

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