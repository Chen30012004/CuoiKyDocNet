using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using CuoiKyDocNet.Models;
using CuoiKyDocNet.Services;
using System;
using System.Text;
using System.Threading.Tasks;

namespace CuoiKyDocNet.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly EmailService _emailService;

        public AccountController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, EmailService emailService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _emailService = emailService;
        }

        [HttpGet]
        public ActionResult Login(string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string email, string password, bool rememberMe, string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                ModelState.AddModelError(string.Empty, "Email and password are required.");
                return View();
            }

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                return View();
            }

            if (!user.EmailConfirmed)
            {
                ModelState.AddModelError(string.Empty, "Email not verified. Please verify your email before logging in.");
                return View();
            }

            var result = await _signInManager.PasswordSignInAsync(user, password, rememberMe, lockoutOnFailure: false);
            if (result.Succeeded)
            {
                // Đăng nhập thành công, lưu thông tin người dùng vào TempData để kiểm tra
                TempData["SuccessMessage"] = "Login successful.";
                return RedirectToLocal(returnUrl);
            }

            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            return View();
        }

        // Các hành động khác giữ nguyên
        [HttpGet]
        public ActionResult SignUp()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SignUp(string email, string password, string fullName)
        {
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    FullName = fullName,
                    EmailConfirmed = false,
                    VerificationCode = GenerateVerificationCode()
                };
                var result = await _userManager.CreateAsync(user, password);
                if (result.Succeeded)
                {
                    try
                    {
                        var emailBody = $"Your verification code is: <strong>{user.VerificationCode}</strong>. Please enter this code in the app to verify your email.";
                        await _emailService.SendEmailAsync(email, "Verify Your Email - Podcastify", emailBody);
                        TempData["VerificationEmail"] = email;
                    }
                    catch (Exception ex)
                    {
                        ModelState.AddModelError(string.Empty, $"Failed to send verification email: {ex.Message}");
                        return View();
                    }
                    return RedirectToAction("VerifyCode");
                }
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
            return View();
        }

        [HttpGet]
        public ActionResult VerifyCode()
        {
            var email = TempData["VerificationEmail"]?.ToString();
            if (string.IsNullOrEmpty(email))
            {
                return RedirectToAction("SignUp");
            }
            TempData.Keep("VerificationEmail");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyCode(string code)
        {
            var email = TempData["VerificationEmail"]?.ToString();
            if (string.IsNullOrEmpty(email))
            {
                return RedirectToAction("SignUp");
            }

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "User not found.");
                return View();
            }

            if (user.VerificationCode == code)
            {
                try
                {
                    user.EmailConfirmed = true;
                    user.VerificationCode = "";
                    var result = await _userManager.UpdateAsync(user);
                    if (result.Succeeded)
                    {
                        await _signInManager.SignInAsync(user, isPersistent: false);
                        TempData.Remove("VerificationEmail");
                        return RedirectToAction("Index", "Home");
                    }
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError(string.Empty, $"Error verifying email: {ex.Message}");
                }
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Invalid verification code.");
                TempData.Keep("VerificationEmail");
            }
            return View();
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Profile(bool edit = false)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login");
            }

            var model = new ProfileViewModel
            {
                FullName = user.FullName,
                Email = user.Email,
                ReceiveEmailNotifications = user.ReceiveEmailNotifications,
                IsEditMode = edit
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Profile(ProfileViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    user.FullName = model.FullName;
                    user.ReceiveEmailNotifications = model.ReceiveEmailNotifications;
                    var result = await _userManager.UpdateAsync(user);
                    if (result.Succeeded)
                    {
                        TempData["SuccessMessage"] = "Profile updated successfully.";
                        return RedirectToAction("Profile", new { edit = false });
                    }
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError(string.Empty, $"Error updating profile: {ex.Message}");
                }
            }

            model.IsEditMode = true;
            return View(model);
        }

        [HttpGet]
        [Authorize]
        public IActionResult ChangePassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login");
            }

            var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "Password changed successfully.";
                return RedirectToAction("Profile");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
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
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null || !user.EmailConfirmed)
            {
                ViewBag.Message = "If the email is registered and verified, a password reset link will be sent to your inbox.";
                return View(model);
            }

            try
            {
                var code = await _userManager.GeneratePasswordResetTokenAsync(user);
                code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                var callbackUrl = Url.Action("ResetPassword", "Account", new { code }, Request.Scheme);

                var emailBody = $"Please reset your password by clicking <a href='{callbackUrl}'>here</a>.";
                await _emailService.SendEmailAsync(model.Email, "Reset Password - Podcastify", emailBody);
                ViewBag.Message = "A password reset link has been sent to your email. Please check your inbox (including spam folder).";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending password reset email: {ex.Message}");
                ModelState.AddModelError(string.Empty, "An error occurred while sending the password reset email. Please try again later.");
            }

            return View(model);
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResetPassword(string code)
        {
            if (code == null)
            {
                return BadRequest("A code must be supplied for password reset.");
            }
            var model = new ResetPasswordViewModel { Code = code };
            return View(model);
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Invalid request.");
                return View(model);
            }

            var code = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(model.Code));
            var result = await _userManager.ResetPasswordAsync(user, code, model.NewPassword);
            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "Your password has been reset successfully.";
                return RedirectToAction("Login");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        private ActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);
            return RedirectToAction("Index", "Home");
        }

        private string GenerateVerificationCode()
        {
            Random random = new Random();
            return random.Next(100000, 999999).ToString();
        }
    }
}