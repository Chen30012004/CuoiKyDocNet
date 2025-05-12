using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using CuoiKyDocNet.Models;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace CuoiKyDocNet.Controllers
{
    [Authorize]
    public class ManageController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ILogger<ManageController> _logger;

        public ManageController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ILogger<ManageController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                _logger.LogWarning("User not found for profile management.");
                return NotFound();
            }

            var model = new EditProfileViewModel
            {
                FullName = user.FullName,
                Email = user.Email,
                ReceiveEmailNotifications = user.ReceiveEmailNotifications
            };

            return View(model);
        }

        [HttpGet]
        public IActionResult EditProfile()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProfile(EditProfileViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("Index", model);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                _logger.LogWarning("User not found for profile update.");
                return NotFound();
            }

            var emailChanged = user.Email != model.Email;
            user.FullName = model.FullName;
            user.Email = model.Email;
            user.ReceiveEmailNotifications = model.ReceiveEmailNotifications;

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                if (emailChanged)
                {
                    user.EmailConfirmed = false;
                    await _userManager.UpdateAsync(user);
                    // Gửi email xác nhận nếu cần
                }

                _logger.LogInformation("User {Email} updated their profile.", user.Email);
                TempData["SuccessMessage"] = "Profile updated successfully.";
                return RedirectToAction("Index");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            return View("Index", model);
        }

        [HttpGet]
        public IActionResult ChangePassword()
        {
            return View(new ChangePasswordViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                _logger.LogWarning("User not found for password change.");
                return NotFound();
            }

            var changePasswordResult = await _userManager.ChangePasswordAsync(user, model.OldPassword, model.NewPassword);
            if (!changePasswordResult.Succeeded)
            {
                foreach (var error in changePasswordResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return View(model);
            }

            await _signInManager.RefreshSignInAsync(user);
            _logger.LogInformation("User {Email} changed their password successfully.", user.Email);
            TempData["SuccessMessage"] = "Your password has been changed successfully.";
            return RedirectToAction("Index");
        }
    }
}