namespace CuoiKyDocNet.Models
{
    public class UserFavoritePodcasts
    {
        public string UserId { get; set; }
        public ApplicationUser User { get; set; }

        public int PodcastId { get; set; }
        public Podcast Podcast { get; set; }
    }
}