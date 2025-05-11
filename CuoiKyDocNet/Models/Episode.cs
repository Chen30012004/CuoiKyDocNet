namespace CuoiKyDocNet.Models
{
    public class Episode
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string AudioUrl { get; set; }
        public DateTime ReleaseDate { get; set; }
        public int Duration { get; set; }
        public int PodcastId { get; set; }
        public Podcast Podcast { get; set; }
    }
}