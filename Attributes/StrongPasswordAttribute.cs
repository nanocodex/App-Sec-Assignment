using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace WebApplication1.Attributes
{
    public class StrongPasswordAttribute : ValidationAttribute
    {
        public StrongPasswordAttribute()
        {
            ErrorMessage = "Password must be at least 12 characters long and contain uppercase, lowercase, numbers, and special characters.";
        }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            var password = value as string;

            if (string.IsNullOrEmpty(password))
            {
                return new ValidationResult("Password is required.");
            }

            var errors = new List<string>();

            // Minimum length check (12 characters)
            if (password.Length < 12)
            {
                errors.Add("at least 12 characters");
            }

            // Check for lowercase letter
            if (!Regex.IsMatch(password, @"[a-z]"))
            {
                errors.Add("a lowercase letter");
            }

            // Check for uppercase letter
            if (!Regex.IsMatch(password, @"[A-Z]"))
            {
                errors.Add("an uppercase letter");
            }

            // Check for digit
            if (!Regex.IsMatch(password, @"\d"))
            {
                errors.Add("a number");
            }

            // Check for special character
            if (!Regex.IsMatch(password, @"[!@#$%^&*()_+\-=\[\]{}|;:'"",.<>?/]"))
            {
                errors.Add("a special character");
            }

            if (errors.Any())
            {
                return new ValidationResult($"Password must contain {string.Join(", ", errors)}.");
            }

            return ValidationResult.Success;
        }
    }
}
