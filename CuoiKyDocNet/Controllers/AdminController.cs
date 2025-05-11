using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CuoiKyDocNet.Data;
using CuoiKyDocNet.Models;
using System.Threading.Tasks;

namespace CuoiKyDocNet.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly PodcastContext _context;

        public AdminController(UserManager<ApplicationUser> userManager, PodcastContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var users = await _userManager.Users.ToListAsync();
            var podcasts = await _context.Podcasts.ToListAsync();
            var viewModel = new AdminViewModel
            {
                Users = users,
                Podcasts = podcasts
            };
            return View(viewModel);
        }

        public async Task<IActionResult> ManageUsers()
        {
            var users = await _userManager.Users.ToListAsync();
            return View(users);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user != null)
            {
                await _userManager.DeleteAsync(user);
                TempData["SuccessMessage"] = "User deleted successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = "User not found.";
            }
            return RedirectToAction("ManageUsers");
        }

        public async Task<IActionResult> ManagePodcasts()
        {
            var podcasts = await _context.Podcasts.ToListAsync();
            return View(podcasts);
        }

        [HttpGet]
        public IActionResult AddPodcast()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> AddPodcast(string title, string description, string imageUrl, string category)
        {
            if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(description) || string.IsNullOrWhiteSpace(category))
            {
                TempData["ErrorMessage"] = "Title, Description, and Category are required.";
                return RedirectToAction("AddPodcast");
            }

            var podcast = new Podcast
            {
                Title = title,
                Description = description,
                ImageUrl = string.IsNullOrWhiteSpace(imageUrl) ? "https://via.placeholder.com/150" : imageUrl, // Giá trị mặc định
                Category = category
            };

            _context.Podcasts.Add(podcast);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Podcast added successfully.";
            return RedirectToAction("ManagePodcasts");
        }

        [HttpGet]
        public IActionResult EditPodcast(int id)
        {
            return RedirectToAction("Details", "Podcasts", new { id });
        }

        [HttpPost]
        public async Task<IActionResult> DeletePodcast(int id)
        {
            var podcast = await _context.Podcasts.FindAsync(id);
            if (podcast != null)
            {
                var favorites = await _context.UserFavoritePodcasts
                    .Where(ufp => ufp.PodcastId == id)
                    .ToListAsync();
                _context.UserFavoritePodcasts.RemoveRange(favorites);

                _context.Podcasts.Remove(podcast);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Podcast deleted successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = "Podcast not found.";
            }
            return RedirectToAction("ManagePodcasts");
        }
    }

    public class AdminViewModel
    {
        public IList<ApplicationUser> Users { get; set; }
        public IList<Podcast> Podcasts { get; set; }
    }
}