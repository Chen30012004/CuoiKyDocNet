using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Identity;
using CuoiKyDocNet.Models;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using CuoiKyDocNet.Data;

namespace CuoiKyDocNet.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ILogger<AdminController> _logger;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly PodcastContext _context;

        public AdminController(ILogger<AdminController> logger, UserManager<ApplicationUser> userManager, PodcastContext context)
        {
            _logger = logger;
            _userManager = userManager;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var users = await _userManager.Users.ToListAsync();
            var podcasts = await _context.Podcasts.ToListAsync();

            var model = new AdminDashboardViewModel
            {
                Users = users,
                Podcasts = podcasts
            };

            _logger.LogInformation("Admin accessed the dashboard.");
            return View(model);
        }

        public async Task<IActionResult> ManageUsers()
        {
            var users = await _userManager.Users.ToListAsync();
            var model = new AdminDashboardViewModel
            {
                Users = users
            };

            _logger.LogInformation("Admin accessed Manage Users page.");
            return View(model);
        }

        public async Task<IActionResult> ManagePodcasts()
        {
            var podcasts = await _context.Podcasts.ToListAsync();
            var model = new AdminDashboardViewModel
            {
                Podcasts = podcasts
            };

            _logger.LogInformation("Admin accessed Manage Podcasts page.");
            return View(model);
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
            if (ModelState.IsValid)
            {
                _context.Podcasts.Add(model);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Podcast {Title} added successfully.", model.Title);
                return RedirectToAction("ManagePodcasts");
            }
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> DetailsPodcast(int id, bool isEditMode = false)
        {
            var podcast = await _context.Podcasts
                .Include(p => p.Episodes)
                .FirstOrDefaultAsync(p => p.Id == id);
            if (podcast == null)
            {
                _logger.LogWarning("DetailsPodcast failed: Podcast with ID {Id} not found.", id);
                return NotFound();
            }
            ViewBag.IsEditMode = isEditMode;
            return View("~/Views/Podcasts/Details.cshtml", podcast);
        }

        [HttpPost]
        public async Task<IActionResult> DetailsPodcast(Podcast model)
        {
            if (ModelState.IsValid)
            {
                var podcast = await _context.Podcasts.FindAsync(model.Id);
                if (podcast == null)
                {
                    _logger.LogWarning("DetailsPodcast failed: Podcast with ID {Id} not found.", model.Id);
                    return NotFound();
                }

                podcast.Title = model.Title;
                podcast.Description = model.Description;
                podcast.ImageUrl = model.ImageUrl;
                podcast.Category = model.Category;
                await _context.SaveChangesAsync();
                _logger.LogInformation("Podcast {Title} updated successfully.", podcast.Title);
                return RedirectToAction("DetailsPodcast", new { id = podcast.Id });
            }
            ViewBag.IsEditMode = true;
            return View("~/Views/Podcasts/Details.cshtml", model);
        }

        [HttpPost]
        public async Task<IActionResult> DeletePodcast(int id)
        {
            var podcast = await _context.Podcasts
                .Include(p => p.Episodes)
                .Include(p => p.UserFavorites)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (podcast == null)
            {
                _logger.LogWarning("DeletePodcast failed: Podcast with ID {Id} not found.", id);
                return NotFound();
            }

            try
            {
                _context.Episodes.RemoveRange(podcast.Episodes);
                _context.UserFavoritePodcasts.RemoveRange(podcast.UserFavorites);
                _context.Podcasts.Remove(podcast);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Podcast {Title} deleted successfully.", podcast.Title);
                return RedirectToAction("ManagePodcasts");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while deleting podcast with ID {Id}.", id);
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
            if (ModelState.IsValid)
            {
                var podcast = await _context.Podcasts.FindAsync(model.PodcastId);
                if (podcast == null)
                {
                    ModelState.AddModelError("", "Podcast not found.");
                    return View(model);
                }

                model.ReleaseDate = DateTime.Now;
                model.Podcast = podcast; // Gán Podcast để thiết lập mối quan hệ
                _context.Episodes.Add(model);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Episode {Title} added to Podcast ID {PodcastId} successfully.", model.Title, model.PodcastId);
                return RedirectToAction("DetailsPodcast", new { id = model.PodcastId });
            }
            return View(model);
        }

        // Các action khác (AddUser, EditUser, DeleteUser, v.v.) giữ nguyên như trước
    }
}