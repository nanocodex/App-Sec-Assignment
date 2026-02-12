using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace WebApplication1.Attributes
{
    /// <summary>
    /// Validates name fields - allows only letters, spaces, hyphens, and apostrophes
    /// Prevents injection attacks and special characters
    /// </summary>
    public class NameValidationAttribute : ValidationAttribute
    {
        public NameValidationAttribute()
        {
            ErrorMessage = "Name can only contain letters, spaces, hyphens, and apostrophes.";
        }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
            {
                return new ValidationResult("Name is required.");
            }

            var name = value.ToString()!.Trim();

            // Name should be reasonable length
            if (name.Length < 2)
            {
                return new ValidationResult("Name must be at least 2 characters long.");
            }

            if (name.Length > 50)
            {
                return new ValidationResult("Name cannot exceed 50 characters.");
            }

            // Allow only letters (including accented characters), spaces, hyphens, and apostrophes
            if (!Regex.IsMatch(name, @"^[\p{L}\s'\-]+$"))
            {
                return new ValidationResult(ErrorMessage);
            }

            // Check for suspicious patterns
            if (Regex.IsMatch(name, @"<|>|script|javascript|onerror|onclick", RegexOptions.IgnoreCase))
            {
                return new ValidationResult("Invalid characters detected in name.");
            }

            // Prevent multiple consecutive special characters
            if (Regex.IsMatch(name, @"['\-]{3,}"))
            {
                return new ValidationResult("Name contains too many consecutive special characters.");
            }

            return ValidationResult.Success;
        }
    }
}
