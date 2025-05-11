using System.Collections.Generic;
using CuoiKyDocNet.Models;

namespace CuoiKyDocNet.Models
{
    public class AdminViewModel
    {
        public string Email { get; set; }
        public string FullName { get; set; }
        public string Role { get; set; }
        public List<Podcast> Podcasts { get; set; }
        public List<ApplicationUser> Users { get; set; } // Thêm thuộc tính Users
    }
}