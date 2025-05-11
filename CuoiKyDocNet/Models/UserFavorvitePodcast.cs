namespace CuoiKyDocNet.Models
{
    public class UserFavoritePodcast
    {
        public string UserId { get; set; }
        public int PodcastId { get; set; }
        public ApplicationUser User { get; set; }
        public Podcast Podcast { get; set; }
    }
}