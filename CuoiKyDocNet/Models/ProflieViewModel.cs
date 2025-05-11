using System.ComponentModel.DataAnnotations;

namespace CuoiKyDocNet.Models
{
    public class ProfileViewModel
    {
        [Required(ErrorMessage = "Full name is required.")]
        [StringLength(100, ErrorMessage = "Full name cannot exceed 100 characters.")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        public string Email { get; set; }

        public bool ReceiveEmailNotifications { get; set; }

        public bool IsEditMode { get; set; } // Trạng thái chỉnh sửa
    }
}