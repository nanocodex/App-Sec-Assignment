using System.ComponentModel.DataAnnotations;
using WebApplication1.Attributes;

namespace WebApplication1.ViewModels
{
    public class ForgotPassword
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        [Display(Name = "Email Address")]
        public required string Email { get; set; }

        [Display(Name = "Reset via SMS")]
        public bool UseSms { get; set; }
    }

    public class ResetPassword
    {
        public required string UserId { get; set; }
        public required string Token { get; set; }

        [Required(ErrorMessage = "New password is required")]
        [DataType(DataType.Password)]
        [StrongPassword]
        [Display(Name = "New Password")]
        public required string NewPassword { get; set; }

        [Required(ErrorMessage = "Please confirm your new password")]
        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "The new password and confirmation password do not match.")]
        [Display(Name = "Confirm New Password")]
        public required string ConfirmPassword { get; set; }
    }

    public class ResetPasswordSms
    {
        [Required(ErrorMessage = "Mobile number is required")]
        [SingaporeMobile]
        [Display(Name = "Mobile Number")]
        public required string Mobile { get; set; }

        [Required(ErrorMessage = "Reset code is required")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "Reset code must be 6 digits")]
        [Display(Name = "Reset Code")]
        public required string ResetCode { get; set; }

        [Required(ErrorMessage = "New password is required")]
        [DataType(DataType.Password)]
        [StrongPassword]
        [Display(Name = "New Password")]
        public required string NewPassword { get; set; }

        [Required(ErrorMessage = "Please confirm your new password")]
        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "The new password and confirmation password do not match.")]
        [Display(Name = "Confirm New Password")]
        public required string ConfirmPassword { get; set; }
    }
}
