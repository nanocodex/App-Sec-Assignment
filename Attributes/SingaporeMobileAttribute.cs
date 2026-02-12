using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace WebApplication1.Attributes
{
    /// <summary>
    /// Validates Singapore mobile phone numbers (8 digits starting with 8 or 9)
    /// </summary>
    public class SingaporeMobileAttribute : ValidationAttribute
    {
        public SingaporeMobileAttribute()
        {
            ErrorMessage = "Mobile number must be 8 digits and start with 8 or 9.";
        }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
            {
                return new ValidationResult("Mobile number is required.");
            }

            var mobile = value.ToString()!.Trim();

            // Remove spaces and dashes for validation
            mobile = Regex.Replace(mobile, @"[\s\-]", string.Empty);

            // Singapore mobile: 8 digits, starts with 8 or 9
            if (!Regex.IsMatch(mobile, @"^[89]\d{7}$"))
            {
                return new ValidationResult(ErrorMessage);
            }

            return ValidationResult.Success;
        }
    }
}
