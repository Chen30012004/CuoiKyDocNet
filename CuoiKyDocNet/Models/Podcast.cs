using System.Collections.Generic;

namespace CuoiKyDocNet.Models
{
    public class Podcast
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string ImageUrl { get; set; }
        public string Category { get; set; }

        public ICollection<UserFavoritePodcast> UserFavorites { get; set; }
        public ICollection<Episode> Episodes { get; set; }

        public Podcast()
        {
            UserFavorites = new List<UserFavoritePodcast>();
            Episodes = new List<Episode>();
        }
    }
}