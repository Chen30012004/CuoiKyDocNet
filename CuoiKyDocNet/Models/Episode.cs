using System.ComponentModel.DataAnnotations;

namespace CuoiKyDocNet.Models
{
    public class Episode
    {
        public int Id { get; set; }

        [Required]
        public string Title { get; set; }

        [Required]
        public string Description { get; set; }

        [Required]
        public string AudioUrl { get; set; }

        public DateTime ReleaseDate { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Duration must be greater than 0.")]
        public int Duration { get; set; }

        [Required]
        public int PodcastId { get; set; }

        public Podcast? Podcast { get; set; }
    }
}