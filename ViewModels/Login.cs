using System.ComponentModel.DataAnnotations;
using WebApplication1.Attributes;

namespace WebApplication1.ViewModels
{
    public class Login
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        [NoHtml]
        [StringLength(100, ErrorMessage = "Email cannot exceed 100 characters")]
        [Display(Name = "Email Address")]
        public required string Email { get; set; }

        [Required(ErrorMessage = "Password is required")]
        [DataType(DataType.Password)]
        [StringLength(100, MinimumLength = 1, ErrorMessage = "Password is required")]
        [Display(Name = "Password")]
        public required string Password { get; set; }

        [Display(Name = "Remember Me")]
        public bool RememberMe { get; set; }
    }
}
