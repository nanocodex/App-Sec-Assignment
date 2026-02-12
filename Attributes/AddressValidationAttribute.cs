using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace WebApplication1.Attributes
{
    /// <summary>
    /// Validates address format - allows alphanumeric, spaces, common punctuation but blocks script injection
    /// </summary>
    public class AddressValidationAttribute : ValidationAttribute
    {
        public AddressValidationAttribute()
        {
            ErrorMessage = "Address contains invalid characters.";
        }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
            {
                return new ValidationResult("Address is required.");
            }

            var address = value.ToString()!.Trim();

            // Address should be reasonable length
            if (address.Length < 5)
            {
                return new ValidationResult("Address is too short. Please enter a complete address.");
            }

            if (address.Length > 200)
            {
                return new ValidationResult("Address is too long. Maximum 200 characters.");
            }

            // Allow alphanumeric, spaces, and common punctuation: . , - # /
            // Block special characters that could be used in injection attacks
            if (!Regex.IsMatch(address, @"^[a-zA-Z0-9\s.,\-#/()]+$"))
            {
                return new ValidationResult("Address can only contain letters, numbers, spaces, and common punctuation (.,#-/).");
            }

            // Check for XSS patterns
            var xssPatterns = new[]
            {
                @"<script",
                @"javascript:",
                @"onerror\s*=",
                @"onclick\s*="
            };

            foreach (var pattern in xssPatterns)
            {
                if (Regex.IsMatch(address, pattern, RegexOptions.IgnoreCase))
                {
                    return new ValidationResult("Invalid characters detected in address.");
                }
            }

            return ValidationResult.Success;
        }
    }
}
