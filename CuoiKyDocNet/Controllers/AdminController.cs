using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Identity;
using CuoiKyDocNet.Models;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using CuoiKyDocNet.Data;
using System;

namespace CuoiKyDocNet.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ILogger<AdminController> _logger;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly PodcastContext _context;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AdminController(
            ILogger<AdminController> logger,
            UserManager<ApplicationUser> userManager,
            PodcastContext context,
            RoleManager<IdentityRole> roleManager)
        {
            _logger = logger;
            _userManager = userManager;
            _context = context;
            _roleManager = roleManager;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var users = await _userManager.Users.ToListAsync();
                var podcasts = await _context.Podcasts.ToListAsync();

                var model = new AdminDashboardViewModel
                {
                    Users = users ?? new List<ApplicationUser>(),
                    Podcasts = podcasts ?? new List<Podcast>()
                };

                _logger.LogInformation("Admin accessed the dashboard.");
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while retrieving data for Admin dashboard.");
                TempData["ErrorMessage"] = "An error occurred while loading the dashboard.";
                return RedirectToAction("Index", "Home");
            }
        }

        public async Task<IActionResult> ManageUsers()
        {
            try
            {
                var users = await _userManager.Users.ToListAsync();
                var model = new AdminDashboardViewModel
                {
                    Users = users ?? new List<ApplicationUser>()
                };

                _logger.LogInformation("Admin accessed Manage Users page.");
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while retrieving users for ManageUsers page.");
                TempData["ErrorMessage"] = "An error occurred while loading the user list.";
                return RedirectToAction("Index");
            }
        }

        public async Task<IActionResult> ManagePodcasts()
        {
            try
            {
                var podcasts = await _context.Podcasts.ToListAsync();
                var model = new AdminDashboardViewModel
                {
                    Podcasts = podcasts ?? new List<Podcast>()
                };

                _logger.LogInformation("Admin accessed Manage Podcasts page.");
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while retrieving podcasts for ManagePodcasts page.");
                TempData["ErrorMessage"] = "An error occurred while loading the podcast list.";
                return RedirectToAction("Index");
            }
        }

        [HttpGet]
        public IActionResult AddPodcast()
        {
            var model = new Podcast();
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> AddPodcast(Podcast model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    _context.Podcasts.Add(model);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Podcast {Title} added successfully.", model.Title);
                    TempData["SuccessMessage"] = "Podcast added successfully.";
                    return RedirectToAction("ManagePodcasts");
                }
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while adding podcast with title {Title}.", model.Title);
                TempData["ErrorMessage"] = "An error occurred while adding the podcast.";
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> DetailsPodcast(int id, bool isEditMode = false)
        {
            try
            {
                var podcast = await _context.Podcasts
                    .Include(p => p.Episodes)
                    .FirstOrDefaultAsync(p => p.Id == id);
                if (podcast == null)
                {
                    _logger.LogWarning("DetailsPodcast failed: Podcast with ID {Id} not found.", id);
                    TempData["ErrorMessage"] = "Podcast not found.";
                    return RedirectToAction("ManagePodcasts");
                }
                ViewBag.IsEditMode = isEditMode;
                return View("~/Views/Podcasts/Details.cshtml", podcast);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while retrieving podcast details for ID {Id}.", id);
                TempData["ErrorMessage"] = "An error occurred while loading podcast details.";
                return RedirectToAction("ManagePodcasts");
            }
        }

        [HttpPost]
        public async Task<IActionResult> DetailsPodcast(Podcast model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var podcast = await _context.Podcasts.FindAsync(model.Id);
                    if (podcast == null)
                    {
                        _logger.LogWarning("DetailsPodcast failed: Podcast with ID {Id} not found.", model.Id);
                        TempData["ErrorMessage"] = "Podcast not found.";
                        return RedirectToAction("ManagePodcasts");
                    }

                    podcast.Title = model.Title;
                    podcast.Description = model.Description;
                    podcast.ImageUrl = model.ImageUrl;
                    podcast.Category = model.Category;
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Podcast {Title} updated successfully.", podcast.Title);
                    TempData["SuccessMessage"] = "Podcast updated successfully.";
                    return RedirectToAction("DetailsPodcast", new { id = podcast.Id });
                }
                ViewBag.IsEditMode = true;
                return View("~/Views/Podcasts/Details.cshtml", model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while updating podcast with ID {Id}.", model.Id);
                TempData["ErrorMessage"] = "An error occurred while updating the podcast.";
                ViewBag.IsEditMode = true;
                return View("~/Views/Podcasts/Details.cshtml", model);
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeletePodcast(int id)
        {
            try
            {
                var podcast = await _context.Podcasts
                    .Include(p => p.Episodes)
                    .Include(p => p.UserFavorites)
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (podcast == null)
                {
                    _logger.LogWarning("DeletePodcast failed: Podcast with ID {Id} not found.", id);
                    TempData["ErrorMessage"] = "Podcast not found.";
                    return RedirectToAction("ManagePodcasts");
                }

                _context.Episodes.RemoveRange(podcast.Episodes);
                _context.UserFavoritePodcasts.RemoveRange(podcast.UserFavorites);
                _context.Podcasts.Remove(podcast);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Podcast {Title} deleted successfully.", podcast.Title);
                TempData["SuccessMessage"] = "Podcast deleted successfully.";
                return RedirectToAction("ManagePodcasts");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while deleting podcast with ID {Id}.", id);
                TempData["ErrorMessage"] = "An error occurred while deleting the podcast.";
                return RedirectToAction("ManagePodcasts");
            }
        }

        [HttpGet]
        public IActionResult AddEpisode(int podcastId)
        {
            var model = new Episode { PodcastId = podcastId };
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> AddEpisode(Episode model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var podcast = await _context.Podcasts.FindAsync(model.PodcastId);
                    if (podcast == null)
                    {
                        _logger.LogWarning("AddEpisode failed: Podcast with ID {PodcastId} not found.", model.PodcastId);
                        ModelState.AddModelError("", "Podcast not found.");
                        return View(model);
                    }

                    model.ReleaseDate = DateTime.Now;
                    model.Podcast = podcast;
                    _context.Episodes.Add(model);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Episode {Title} added to Podcast ID {PodcastId} successfully.", model.Title, model.PodcastId);
                    TempData["SuccessMessage"] = "Episode added successfully.";
                    return RedirectToAction("DetailsPodcast", new { id = model.PodcastId });
                }
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while adding episode with title {Title} to Podcast ID {PodcastId}.", model.Title, model.PodcastId);
                TempData["ErrorMessage"] = "An error occurred while adding the episode.";
                return View(model);
            }
        }

        [HttpGet]
        public IActionResult AddUser()
        {
            var model = new EditUserViewModel();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddUser(EditUserViewModel model)
        {
            try
            {
                // Bỏ qua validation của Id
                ModelState.Remove("Id");

                if (ModelState.IsValid)
                {
                    var existingUser = await _userManager.FindByEmailAsync(model.Email);
                    if (existingUser != null)
                    {
                        _logger.LogWarning("AddUser failed: Email {Email} already registered.", model.Email);
                        ModelState.AddModelError(string.Empty, "This email is already registered.");
                        return View(model);
                    }

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
                        EmailConfirmed = model.EmailConfirmed,
                        ReceiveEmailNotifications = model.ReceiveEmailNotifications
                    };

                    var result = await _userManager.CreateAsync(user, model.Password);
                    if (result.Succeeded)
                    {
                        var role = model.Role == "Admin" ? "Admin" : "User";
                        await _userManager.AddToRoleAsync(user, role);
                        _logger.LogInformation("User {Email} added successfully with role {Role} by admin.", user.Email, role);
                        TempData["SuccessMessage"] = "User added successfully.";
                        return RedirectToAction("ManageUsers");
                    }

                    foreach (var error in result.Errors)
                    {
                        _logger.LogError("AddUser error for {Email}: {Error}", model.Email, error.Description);
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                }
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while adding user with email {Email}.", model.Email);
                TempData["ErrorMessage"] = "An error occurred while adding the user.";
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> EditUser(string id)
        {
            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    _logger.LogWarning("EditUser failed: Invalid user ID.");
                    TempData["ErrorMessage"] = "Invalid user ID.";
                    return RedirectToAction("ManageUsers");
                }

                var user = await _userManager.FindByIdAsync(id);
                if (user == null)
                {
                    _logger.LogWarning("EditUser failed: User with ID {Id} not found.", id);
                    TempData["ErrorMessage"] = "User not found.";
                    return RedirectToAction("ManageUsers");
                }

                var roles = await _userManager.GetRolesAsync(user);
                var role = roles.FirstOrDefault() ?? "User"; // Lấy vai trò đầu tiên (User hoặc Admin)

                var model = new EditUserViewModel
                {
                    Id = user.Id,
                    Email = user.Email,
                    FullName = user.FullName,
                    EmailConfirmed = user.EmailConfirmed,
                    ReceiveEmailNotifications = user.ReceiveEmailNotifications,
                    Role = role
                };

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while retrieving user with ID {Id} for editing.", id);
                TempData["ErrorMessage"] = "An error occurred while loading the user for editing.";
                return RedirectToAction("ManageUsers");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUser(EditUserViewModel model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var user = await _userManager.FindByIdAsync(model.Id);
                    if (user == null)
                    {
                        _logger.LogWarning("EditUser failed: User with ID {Id} not found.", model.Id);
                        TempData["ErrorMessage"] = "User not found.";
                        return RedirectToAction("ManageUsers");
                    }

                    var existingUser = await _userManager.FindByEmailAsync(model.Email);
                    if (existingUser != null && existingUser.Id != model.Id)
                    {
                        _logger.LogWarning("EditUser failed: Email {Email} is already registered.", model.Email);
                        ModelState.AddModelError(string.Empty, "This email is already registered by another user.");
                        return View(model);
                    }

                    user.Email = model.Email;
                    user.FullName = model.FullName;
                    user.EmailConfirmed = model.EmailConfirmed;
                    user.ReceiveEmailNotifications = model.ReceiveEmailNotifications;

                    // Chỉ cập nhật mật khẩu nếu người dùng nhập mật khẩu mới
                    if (!string.IsNullOrEmpty(model.Password))
                    {
                        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                        var result = await _userManager.ResetPasswordAsync(user, token, model.Password);
                        if (!result.Succeeded)
                        {
                            foreach (var error in result.Errors)
                            {
                                _logger.LogError("EditUser password reset error for {Email}: {Error}", user.Email, error.Description);
                                ModelState.AddModelError(string.Empty, error.Description);
                            }
                            return View(model);
                        }
                    }

                    var currentRoles = await _userManager.GetRolesAsync(user);
                    var newRole = model.Role == "Admin" ? "Admin" : "User";
                    if (currentRoles.FirstOrDefault() != newRole)
                    {
                        await _userManager.RemoveFromRolesAsync(user, currentRoles);
                        await _userManager.AddToRoleAsync(user, newRole);
                    }

                    var updateResult = await _userManager.UpdateAsync(user);
                    if (updateResult.Succeeded)
                    {
                        _logger.LogInformation("User {Email} updated successfully by admin.", user.Email);
                        TempData["SuccessMessage"] = "User updated successfully.";
                        return RedirectToAction("ManageUsers");
                    }

                    foreach (var error in updateResult.Errors)
                    {
                        _logger.LogError("EditUser update error for {Email}: {Error}", user.Email, error.Description);
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                }
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while updating user with ID {Id}.", model.Id);
                TempData["ErrorMessage"] = "An error occurred while updating the user.";
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> DeleteUser(string id)
        {
            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    _logger.LogWarning("DeleteUser failed: User ID is null or empty.");
                    TempData["ErrorMessage"] = "Invalid user ID.";
                    return RedirectToAction("ManageUsers");
                }

                // Kiểm tra xem người dùng đang xóa có phải là chính họ không
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser.Id == id)
                {
                    _logger.LogWarning("DeleteUser failed: Admin with ID {UserId} attempted to delete themselves.", id);
                    TempData["ErrorMessage"] = "You cannot delete your own account.";
                    return RedirectToAction("ManageUsers");
                }

                var user = await _userManager.FindByIdAsync(id);
                if (user == null)
                {
                    _logger.LogWarning("DeleteUser failed: User with ID {UserId} not found.", id);
                    TempData["ErrorMessage"] = "User not found.";
                    return RedirectToAction("ManageUsers");
                }

                var result = await _userManager.DeleteAsync(user);
                if (result.Succeeded)
                {
                    _logger.LogInformation("User with ID {UserId} deleted successfully.", id);
                    TempData["SuccessMessage"] = "User deleted successfully.";
                }
                else
                {
                    _logger.LogError("DeleteUser failed for ID {UserId}. Errors: {Errors}", id, string.Join(", ", result.Errors.Select(e => e.Description)));
                    TempData["ErrorMessage"] = "Failed to delete user: " + string.Join(", ", result.Errors.Select(e => e.Description));
                }

                return RedirectToAction("ManageUsers");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while deleting user with ID {UserId}.", id);
                TempData["ErrorMessage"] = "An error occurred while deleting the user.";
                return RedirectToAction("ManageUsers");
            }
        }
    }
}