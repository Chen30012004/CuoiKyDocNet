using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using CuoiKyDocNet.Data;
using CuoiKyDocNet.Models;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace CuoiKyDocNet.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly PodcastContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ILogger<AdminController> _logger;

        public AdminController(
            PodcastContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ILogger<AdminController> logger)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> ManagePodcasts()
        {
            var podcasts = await _context.Podcasts.ToListAsync();
            return View(podcasts);
        }

        public async Task<IActionResult> EditPodcast(int? id)
        {
            if (id == null || id == 0)
            {
                return View(new Podcast());
            }
            var podcast = await _context.Podcasts.FindAsync(id);
            if (podcast == null)
            {
                _logger.LogWarning("EditPodcast failed: Podcast ID {Id} not found.", id);
                return NotFound();
            }
            return View(podcast);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPodcast(Podcast podcast)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    if (podcast.Id == 0)
                    {
                        _context.Add(podcast);
                        _logger.LogInformation("Podcast {Title} created successfully.", podcast.Title);
                    }
                    else
                    {
                        _context.Update(podcast);
                        _logger.LogInformation("Podcast {Title} updated successfully.", podcast.Title);
                    }
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Podcast saved successfully.";
                    return RedirectToAction("ManagePodcasts");
                }
                catch (DbUpdateException ex)
                {
                    _logger.LogError(ex, "Error saving podcast {Title}.", podcast.Title);
                    ModelState.AddModelError("", "Unable to save changes. Please try again.");
                }
            }
            return View(podcast);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePodcast(int id)
        {
            var podcast = await _context.Podcasts.FindAsync(id);
            if (podcast == null)
            {
                _logger.LogWarning("DeletePodcast failed: Podcast ID {Id} not found.", id);
                return NotFound();
            }

            try
            {
                _context.Podcasts.Remove(podcast);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Podcast {Title} deleted successfully.", podcast.Title);
                TempData["SuccessMessage"] = "Podcast deleted successfully.";
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Error deleting podcast {Title}.", podcast.Title);
                TempData["ErrorMessage"] = "Unable to delete podcast. Please try again.";
            }
            return RedirectToAction("ManagePodcasts");
        }

        public async Task<IActionResult> ManageUsers()
        {
            var users = await _userManager.Users.ToListAsync();
            return View(users);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                _logger.LogWarning("DeleteUser failed: User ID {Id} not found.", id);
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction("ManageUsers");
            }

            var result = await _userManager.DeleteAsync(user);
            if (result.Succeeded)
            {
                _logger.LogInformation("User {Email} deleted successfully.", user.Email);
                TempData["SuccessMessage"] = "User deleted successfully.";
            }
            else
            {
                _logger.LogError("Failed to delete user {Email}.", user.Email);
                TempData["ErrorMessage"] = "Failed to delete user.";
            }
            return RedirectToAction("ManageUsers");
        }

        public async Task<IActionResult> ManageRoles(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("ManageRoles failed: User ID {Id} not found.", userId);
                return NotFound();
            }
            var roles = await _roleManager.Roles.ToListAsync();
            var userRoles = await _userManager.GetRolesAsync(user);
            var model = new ManageRolesViewModel
            {
                UserId = userId,
                Email = user.Email,
                Roles = roles.Select(r => new RoleViewModel
                {
                    RoleName = r.Name,
                    IsAssigned = userRoles.Contains(r.Name)
                }).ToList()
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ManageRoles(ManageRolesViewModel model)
        {
            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user == null)
            {
                _logger.LogWarning("ManageRoles failed: User ID {Id} not found.", model.UserId);
                return NotFound();
            }
            var userRoles = await _userManager.GetRolesAsync(user);
            foreach (var role in model.Roles)
            {
                if (role.IsAssigned && !userRoles.Contains(role.RoleName))
                {
                    await _userManager.AddToRoleAsync(user, role.RoleName);
                    _logger.LogInformation("Role {RoleName} assigned to user {Email}.", role.RoleName, user.Email);
                }
                else if (!role.IsAssigned && userRoles.Contains(role.RoleName))
                {
                    await _userManager.RemoveFromRoleAsync(user, role.RoleName);
                    _logger.LogInformation("Role {RoleName} removed from user {Email}.", role.RoleName, user.Email);
                }
            }
            TempData["SuccessMessage"] = "Roles updated successfully.";
            return RedirectToAction("ManageUsers");
        }
    }
}