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
    public class PodcastsController : Controller
    {
        private readonly PodcastContext _context;
        private readonly ILogger<PodcastsController> _logger;
        private readonly UserManager<ApplicationUser> _userManager;

        public PodcastsController(PodcastContext context, ILogger<PodcastsController> logger, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _logger = logger;
            _userManager = userManager;
        }

        [AllowAnonymous]
        public async Task<IActionResult> Index(string category, int page = 1, int pageSize = 9)
        {
            IQueryable<Podcast> podcasts = _context.Podcasts;
            if (!string.IsNullOrEmpty(category))
            {
                podcasts = podcasts.Where(p => p.Category == category);
            }

            var totalItems = await podcasts.CountAsync();
            var podcastList = await podcasts
                .OrderBy(p => p.Title)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.CurrentCategory = category;
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            _logger.LogInformation("Displayed podcasts with category {Category}, page {Page}.", category ?? "All", page);
            return View(podcastList);
        }

        [AllowAnonymous]
        public async Task<IActionResult> Search(string query)
        {
            IQueryable<Podcast> podcasts = _context.Podcasts;
            if (!string.IsNullOrEmpty(query))
            {
                query = query.ToLower();
                podcasts = podcasts.Where(p => p.Title.ToLower().Contains(query) ||
                                              p.Description.ToLower().Contains(query) ||
                                              p.Category.ToLower().Contains(query));
            }
            var podcastList = await podcasts.ToListAsync();
            ViewBag.Query = query;

            _logger.LogInformation("Search performed with query '{Query}'. Found {Count} results.", query, podcastList.Count);
            return View("Index", podcastList);
        }

        [AllowAnonymous]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                _logger.LogWarning("Podcast details requested with null ID.");
                return NotFound();
            }

            var podcast = await _context.Podcasts
                .Include(p => p.Episodes)
                .Include(p => p.UserFavorites) // Bao gồm thông tin yêu thích
                .FirstOrDefaultAsync(m => m.Id == id);

            if (podcast == null)
            {
                _logger.LogWarning("Podcast ID {Id} not found.", id);
                return NotFound();
            }

            _logger.LogInformation("Displayed details for podcast {Title}.", podcast.Title);
            return View(podcast);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleFavorite(int podcastId, string returnUrl)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                _logger.LogWarning("ToggleFavorite failed: User not authenticated.");
                return Unauthorized();
            }

            var podcast = await _context.Podcasts.FindAsync(podcastId);
            if (podcast == null)
            {
                _logger.LogWarning("ToggleFavorite failed: Podcast ID {Id} not found.", podcastId);
                return NotFound();
            }

            var existingFavorite = await _context.UserFavoritePodcasts
                .FirstOrDefaultAsync(uf => uf.UserId == user.Id && uf.PodcastId == podcastId);

            if (existingFavorite != null)
            {
                // Xóa khỏi danh sách yêu thích
                _context.UserFavoritePodcasts.Remove(existingFavorite);
                TempData["SuccessMessage"] = "Podcast removed from favorites.";
                _logger.LogInformation("User {UserId} unfavorited podcast {PodcastId}.", user.Id, podcastId);
            }
            else
            {
                // Thêm vào danh sách yêu thích
                var favorite = new UserFavoritePodcasts
                {
                    UserId = user.Id,
                    PodcastId = podcastId
                };
                _context.UserFavoritePodcasts.Add(favorite);
                TempData["SuccessMessage"] = "Podcast added to favorites.";
                _logger.LogInformation("User {UserId} favorited podcast {PodcastId}.", user.Id, podcastId);
            }

            await _context.SaveChangesAsync();
            return Redirect(returnUrl);
        }
    }
}