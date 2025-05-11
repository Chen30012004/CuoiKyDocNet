namespace CuoiKyDocNet.Models
{
    public class AdminDashboardViewModel
    {
        public List<ApplicationUser> Users { get; set; }
        public List<Podcast> Podcasts { get; set; }
        public List<Episode> Episodes { get; set; }
    }
}