using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace WebApplication1.Attributes
{
    /// <summary>
    /// Validates address format - allows all printable characters but blocks script injection attempts
    /// Security is provided by HTML encoding on storage and display, not input restriction
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

            // Block null characters and control characters (except newline, carriage return, tab)
            if (Regex.IsMatch(address, @"[\x00-\x08\x0B\x0C\x0E-\x1F\x7F]"))
            {
                return new ValidationResult("Address contains invalid control characters.");
            }

            // Check for obvious XSS attack patterns
            var xssPatterns = new[]
            {
                @"<script[\s\S]*?>[\s\S]*?</script>",
                @"javascript\s*:",
                @"onerror\s*=",
                @"onload\s*=",
                @"onclick\s*=",
                @"onmouseover\s*=",
                @"<iframe[\s\S]*?>",
                @"<embed[\s\S]*?>",
                @"<object[\s\S]*?>",
                @"eval\s*\(",
                @"expression\s*\(",
                @"vbscript\s*:",
                @"data\s*:\s*text/html"
            };

            foreach (var pattern in xssPatterns)
            {
                if (Regex.IsMatch(address, pattern, RegexOptions.IgnoreCase))
                {
                    return new ValidationResult("Address contains potentially dangerous script patterns. Please remove script tags or event handlers.");
                }
            }

            return ValidationResult.Success;
        }
    }
}
