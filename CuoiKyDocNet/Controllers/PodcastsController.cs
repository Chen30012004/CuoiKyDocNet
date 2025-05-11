using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CuoiKyDocNet.Data;
using CuoiKyDocNet.Models;
using System.Threading.Tasks;

namespace CuoiKyDocNet.Controllers
{
    [Authorize(Roles = "Admin")]
    public class PodcastsController : Controller
    {
        private readonly PodcastContext _context;

        public PodcastsController(PodcastContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> ManagePodcasts()
        {
            var podcasts = await _context.Podcasts.ToListAsync();
            return View(podcasts);
        }

        public async Task<IActionResult> Index(string category)
        {
            IQueryable<Podcast> podcasts = _context.Podcasts;

            if (!string.IsNullOrEmpty(category))
            {
                podcasts = podcasts.Where(p => p.Category == category);
            }

            var podcastList = await podcasts.ToListAsync();
            return View("ManagePodcasts", podcastList); // Sử dụng cùng view ManagePodcasts
        }

        public IActionResult AddPodcast()
        {
            return View("EditPodcast", new Podcast());
        }

        [HttpGet]
        public async Task<IActionResult> EditPodcast(int? id)
        {
            if (id == null)
            {
                return View(new Podcast());
            }

            var podcast = await _context.Podcasts.FindAsync(id);
            if (podcast == null)
            {
                return NotFound();
            }
            return View(podcast);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPodcast(int id, string title, string description, string imageUrl, string category, string action)
        {
            var podcast = id == 0 ? new Podcast() : await _context.Podcasts.FindAsync(id);

            if (podcast == null && id != 0)
            {
                return NotFound();
            }

            if (action == "Delete")
            {
                if (podcast != null)
                {
                    _context.Podcasts.Remove(podcast);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Podcast deleted successfully.";
                    return RedirectToAction("ManagePodcasts");
                }
                TempData["ErrorMessage"] = "Podcast not found.";
                return RedirectToAction("ManagePodcasts");
            }

            podcast.Title = title;
            podcast.Description = description;
            podcast.ImageUrl = imageUrl;
            podcast.Category = category;

            if (ModelState.IsValid)
            {
                try
                {
                    if (id == 0)
                    {
                        _context.Add(podcast);
                        TempData["SuccessMessage"] = "Podcast added successfully.";
                    }
                    else
                    {
                        _context.Update(podcast);
                        TempData["SuccessMessage"] = "Podcast updated successfully.";
                    }
                    await _context.SaveChangesAsync();
                    return RedirectToAction("ManagePodcasts");
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PodcastExists(podcast.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            return View(podcast);
        }

        [HttpGet]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var podcast = await _context.Podcasts.FindAsync(id);
            if (podcast == null)
            {
                return NotFound();
            }

            return View(podcast);
        }

        private bool PodcastExists(int id)
        {
            return _context.Podcasts.Any(e => e.Id == id);
        }
    }
}