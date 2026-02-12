using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace WebApplication1.Attributes
{
    /// <summary>
    /// Validates that input does not contain SQL injection patterns
    /// Note: This is defense in depth - Entity Framework parameterized queries are the primary protection
    /// </summary>
    public class NoSqlInjectionAttribute : ValidationAttribute
    {
        public NoSqlInjectionAttribute()
        {
            ErrorMessage = "Invalid characters detected in input.";
        }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
            {
                return ValidationResult.Success;
            }

            var input = value.ToString()!;

            // Check for SQL injection patterns
            var sqlPatterns = new[]
            {
                @"(\b(SELECT|INSERT|UPDATE|DELETE|DROP|CREATE|ALTER|EXEC|EXECUTE|UNION|DECLARE)\b)",
                @"(;|\/\*|\*\/|xp_|sp_)",
                @"'\s*(OR|AND)\s*'?\s*\d+\s*=\s*\d+",
                @"'\s*OR\s*'1'\s*=\s*'1",
                @"--\s*$",
                @";\s*(DROP|DELETE|INSERT|UPDATE)"
            };

            foreach (var pattern in sqlPatterns)
            {
                if (Regex.IsMatch(input, pattern, RegexOptions.IgnoreCase))
                {
                    return new ValidationResult(ErrorMessage);
                }
            }

            return ValidationResult.Success;
        }
    }
}
