using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CuoiKyDocNet.Data;
using CuoiKyDocNet.Models;
using System.Threading.Tasks;

namespace CuoiKyDocNet.Controllers
{
    [Authorize(Roles = "Admin")]
    public class EpisodesController : Controller
    {
        private readonly PodcastContext _context;

        public EpisodesController(PodcastContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Create(int podcastId)
        {
            var podcast = await _context.Podcasts.FindAsync(podcastId);
            if (podcast == null)
            {
                return NotFound();
            }

            var episode = new Episode
            {
                PodcastId = podcastId,
                ReleaseDate = DateTime.Now
            };
            ViewBag.PodcastTitle = podcast.Title;
            return View(episode);
        }

        [HttpPost]
        public async Task<IActionResult> Create(Episode episode, IFormFile audioFile)
        {
            if (ModelState.IsValid)
            {
                if (audioFile != null)
                {
                    using (var stream = audioFile.OpenReadStream())
                    using (var reader = new BinaryReader(stream))
                    {
                        var fileContent = reader.ReadBytes((int)audioFile.Length);
                        // Lưu fileContent vào cơ sở dữ liệu hoặc xử lý theo nhu cầu
                        episode.AudioUrl = $"uploads/{audioFile.FileName}"; // Ví dụ: lưu đường dẫn
                                                                            // Lưu file vào wwwroot/uploads
                        var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads", audioFile.FileName);
                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await audioFile.CopyToAsync(fileStream);
                        }
                    }
                }
                _context.Add(episode);
                await _context.SaveChangesAsync();
                return RedirectToAction("Index");
            }
            return View(episode);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddEpisode(int podcastId, string title, string description, string audioUrl, DateTime releaseDate, int duration)
        {
            if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(audioUrl) || duration <= 0)
            {
                TempData["ErrorMessage"] = "Title, Audio URL, and Duration are required, and Duration must be greater than 0.";
                return RedirectToAction("Details", "Podcasts", new { id = podcastId });
            }

            var episode = new Episode
            {
                PodcastId = podcastId,
                Title = title,
                Description = description,
                AudioUrl = audioUrl,
                ReleaseDate = releaseDate,
                Duration = duration
            };

            _context.Add(episode);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Episode added successfully.";
            return RedirectToAction("Details", "Podcasts", new { id = podcastId });
        }
    }
}