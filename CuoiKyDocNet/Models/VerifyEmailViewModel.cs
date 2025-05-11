using System.ComponentModel.DataAnnotations;

namespace CuoiKyDocNet.Models
{
    public class VerifyEmailViewModel
    {
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email address.")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Verification code is required.")]
        public string Code { get; set; }
    }
}