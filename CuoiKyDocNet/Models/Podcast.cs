using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CuoiKyDocNet.Models
{
    public class Podcast
    {
        public int Id { get; set; }

        [Required]
        public string Title { get; set; }

        [Required]
        public string Description { get; set; }

        [Required]
        public string ImageUrl { get; set; }

        [Required]
        public string Category { get; set; }

        public ICollection<UserFavoritePodcasts> UserFavorites { get; set; }
        public ICollection<Episode> Episodes { get; set; }

    }
}