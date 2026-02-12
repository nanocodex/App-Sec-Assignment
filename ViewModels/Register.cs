using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;
using WebApplication1.Attributes;

namespace WebApplication1.ViewModels
{
    public class Register
    {
        [Required(ErrorMessage = "First name is required")]
        [NameValidation]
        [Display(Name = "First Name")]
        public required string FirstName { get; set; }

        [Required(ErrorMessage = "Last name is required")]
        [NameValidation]
        [Display(Name = "Last Name")]
        public required string LastName { get; set; }

        [Required(ErrorMessage = "Credit card number is required")]
        [CreditCard(ErrorMessage = "Please enter a valid credit card number")]
        [StringLength(19, MinimumLength = 13, ErrorMessage = "Credit card number must be between 13 and 19 digits")]
        [Display(Name = "Credit Card Number")]
        public required string CreditCard { get; set; }

        [Required(ErrorMessage = "Mobile number is required")]
        [SingaporeMobile]
        [Display(Name = "Mobile Number")]
        public required string Mobile { get; set; }

        [Required(ErrorMessage = "Billing address is required")]
        [AddressValidation]
        [Display(Name = "Billing Address")]
        public required string Billing { get; set; }

        [Required(ErrorMessage = "Shipping address is required")]
        [AddressValidation]
        [Display(Name = "Shipping Address")]
        public required string Shipping { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address")]
        [NoHtml]
        [StringLength(100, ErrorMessage = "Email cannot exceed 100 characters")]
        [Display(Name = "Email Address")]
        public required string Email { get; set; }

        [Required(ErrorMessage = "Password is required")]
        [DataType(DataType.Password)]
        [StrongPassword]
        [Display(Name = "Password")]
        public required string Password { get; set; }

        [Required(ErrorMessage = "Password confirmation is required")]
        [DataType(DataType.Password)]
        [Compare(nameof(Password), ErrorMessage = "Password and confirmation password do not match")]
        [Display(Name = "Confirm Password")]
        public required string ConfirmPassword { get; set; }

        [Required(ErrorMessage = "Photo is required")]
        [Display(Name = "Profile Photo")]
        public required IFormFile Photo { get; set; }
    }
}
