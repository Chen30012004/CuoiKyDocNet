using System.ComponentModel.DataAnnotations;

namespace CuoiKyDocNet.Models
{
    public class EditUserViewModel
    {
        public string Id { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string FullName { get; set; }

        public bool EmailConfirmed { get; set; }

        [Display(Name = "Receive Email Notifications")]
        public bool ReceiveEmailNotifications { get; set; }
    }
}