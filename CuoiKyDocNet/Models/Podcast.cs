namespace CuoiKyDocNet.Models
{
    public class Podcast
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string ImageUrl { get; set; }
        public string Category { get; set; }
        public List<Episode> Episodes { get; set; }
    }
}
