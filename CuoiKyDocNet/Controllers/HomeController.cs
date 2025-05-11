using System.Diagnostics;
using CuoiKyDocNet.Data;
using CuoiKyDocNet.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CuoiKyDocNet.Controllers
{
    public class HomeController : Controller
    {
        private readonly PodcastContext _context;

        public HomeController(PodcastContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var podcasts = await _context.Podcasts
                .OrderByDescending(p => p.Id)
                .Take(5)
                .ToListAsync();

            return View(podcasts ?? new List<Podcast>());
        }
    }
}
