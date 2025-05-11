using System.ComponentModel.DataAnnotations;

namespace CuoiKyDocNet.Models
{
    public class EditProfileViewModel
    {
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email address.")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Full name is required.")]
        public string FullName { get; set; }

        [Display(Name = "Receive email notifications")]
        public bool ReceiveEmailNotifications { get; set; }
    }
}