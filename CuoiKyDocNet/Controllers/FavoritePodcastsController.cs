using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CuoiKyDocNet.Data;
using CuoiKyDocNet.Models;
using System.Threading.Tasks;
using System.Linq;

namespace CuoiKyDocNet.Controllers
{
    [Authorize]
    public class FavoritePodcastsController : Controller
    {
        private readonly PodcastContext _context;

        public FavoritePodcastsController(PodcastContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var favoritePodcasts = await _context.UserFavoritePodcasts
                .Where(ufp => ufp.UserId == userId)
                .Include(ufp => ufp.Podcast)
                .Select(ufp => ufp.Podcast)
                .ToListAsync();

            return View(favoritePodcasts);
        }

        [HttpPost]
        public async Task<IActionResult> AddToFavorites(int podcastId)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var existingFavorite = await _context.UserFavoritePodcasts
                .FirstOrDefaultAsync(ufp => ufp.UserId == userId && ufp.PodcastId == podcastId);

            if (existingFavorite == null)
            {
                var favorite = new UserFavoritePodcasts
                {
                    UserId = userId,
                    PodcastId = podcastId
                };
                _context.UserFavoritePodcasts.Add(favorite);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Podcast added to favorites.";
            }
            else
            {
                TempData["ErrorMessage"] = "Podcast is already in your favorites.";
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> RemoveFromFavorites(int podcastId)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var favorite = await _context.UserFavoritePodcasts
                .FirstOrDefaultAsync(ufp => ufp.UserId == userId && ufp.PodcastId == podcastId);

            if (favorite != null)
            {
                _context.UserFavoritePodcasts.Remove(favorite);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Podcast removed from favorites.";
            }

            return RedirectToAction("Index");
        }
    }
}