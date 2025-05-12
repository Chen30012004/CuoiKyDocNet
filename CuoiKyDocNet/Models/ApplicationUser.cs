using Microsoft.AspNetCore.Identity;

namespace CuoiKyDocNet.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; }
        public string? VerificationCode { get; set; }
        public bool ReceiveEmailNotifications { get; set; }
        public ICollection<UserFavoritePodcasts> UserFavorites { get; set; }
    }
}