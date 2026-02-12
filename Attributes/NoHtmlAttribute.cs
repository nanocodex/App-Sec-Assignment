using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace WebApplication1.Attributes
{
    /// <summary>
    /// Validates that input does not contain HTML, script tags, or potential XSS attacks
    /// </summary>
    public class NoHtmlAttribute : ValidationAttribute
    {
        public NoHtmlAttribute()
        {
            ErrorMessage = "This field cannot contain HTML or script content.";
        }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
            {
                return ValidationResult.Success;
            }

            var input = value.ToString()!;

            // Check for HTML tags
            if (Regex.IsMatch(input, @"<[^>]+>", RegexOptions.IgnoreCase))
            {
                return new ValidationResult("HTML tags are not allowed.");
            }

            // Check for common XSS patterns
            var xssPatterns = new[]
            {
                @"javascript:",
                @"onerror\s*=",
                @"onload\s*=",
                @"onclick\s*=",
                @"onmouseover\s*=",
                @"<script",
                @"</script>",
                @"<iframe",
                @"eval\s*\(",
                @"expression\s*\(",
                @"vbscript:",
                @"data:text/html"
            };

            foreach (var pattern in xssPatterns)
            {
                if (Regex.IsMatch(input, pattern, RegexOptions.IgnoreCase))
                {
                    return new ValidationResult("Invalid characters or patterns detected.");
                }
            }

            return ValidationResult.Success;
        }
    }
}
