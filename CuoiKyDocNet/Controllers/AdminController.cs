using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Identity;
using CuoiKyDocNet.Models;
using System.Threading.Tasks;
using System.Linq;

namespace CuoiKyDocNet.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ILogger<AdminController> _logger;
        private readonly UserManager<ApplicationUser> _userManager;

        public AdminController(ILogger<AdminController> logger, UserManager<ApplicationUser> userManager)
        {
            _logger = logger;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var users = _userManager.Users.ToList(); // Lấy danh sách người dùng
            var model = new AdminDashboardViewModel
            {
                Users = users
            };

            _logger.LogInformation("Admin accessed the dashboard.");
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> EditUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                _logger.LogWarning("EditUser failed: User with ID {Id} not found.", id);
                return NotFound();
            }

            var model = new EditUserViewModel
            {
                Id = user.Id,
                Email = user.Email,
                FullName = user.FullName,
                EmailConfirmed = user.EmailConfirmed,
                ReceiveEmailNotifications = user.ReceiveEmailNotifications
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> EditUser(EditUserViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByIdAsync(model.Id);
                if (user == null)
                {
                    _logger.LogWarning("EditUser failed: User with ID {Id} not found.", model.Id);
                    return NotFound();
                }

                user.FullName = model.FullName;
                user.ReceiveEmailNotifications = model.ReceiveEmailNotifications;
                user.EmailConfirmed = model.EmailConfirmed;

                var result = await _userManager.UpdateAsync(user);
                if (result.Succeeded)
                {
                    _logger.LogInformation("User {Email} updated successfully.", user.Email);
                    return RedirectToAction("Index");
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                _logger.LogWarning("DeleteUser failed: User with ID {Id} not found.", id);
                return NotFound();
            }

            var result = await _userManager.DeleteAsync(user);
            if (result.Succeeded)
            {
                _logger.LogInformation("User {Email} deleted successfully.", user.Email);
                return RedirectToAction("Index");
            }

            foreach (var error in result.Errors)
            {
                _logger.LogError("DeleteUser failed for user with ID {Id}: {Error}", id, error.Description);
            }
            return RedirectToAction("Index");
        }
    }
}