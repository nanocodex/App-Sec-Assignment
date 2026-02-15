using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace WebApplication1.Attributes
{
    /// <summary>
    /// Validates Singapore mobile phone numbers (8 digits starting with 8 or 9)
    /// or international mobile numbers with supported country codes (+65, +60, +62)
    /// </summary>
    public class SingaporeMobileAttribute : ValidationAttribute
    {
        public SingaporeMobileAttribute()
        {
            ErrorMessage = "Mobile number must be 8 digits starting with 8 or 9, or include a valid country code (+65, +60, +62).";
        }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
            {
                return new ValidationResult("Mobile number is required.");
            }

            var mobile = value.ToString()!.Trim();

            // Remove spaces for validation
            mobile = Regex.Replace(mobile, @"\s", string.Empty);

            // Check if it starts with +
            if (mobile.StartsWith("+"))
            {
                // Validate specific country codes
                if (mobile.StartsWith("+65"))
                {
                    // Singapore: +65 followed by 8 digits
                    if (!Regex.IsMatch(mobile, @"^\+65[89]\d{7}$"))
                    {
                        return new ValidationResult("Singapore mobile number must be +65 followed by 8 digits starting with 8 or 9.");
                    }
                }
                else if (mobile.StartsWith("+60"))
                {
                    // Malaysia: +60 followed by 9-10 digits
                    if (!Regex.IsMatch(mobile, @"^\+60\d{9,10}$"))
                    {
                        return new ValidationResult("Malaysia mobile number must be +60 followed by 9-10 digits.");
                    }
                }
                else if (mobile.StartsWith("+62"))
                {
                    // Indonesia: +62 followed by 9-12 digits
                    if (!Regex.IsMatch(mobile, @"^\+62\d{9,12}$"))
                    {
                        return new ValidationResult("Indonesia mobile number must be +62 followed by 9-12 digits.");
                    }
                }
                else
                {
                    return new ValidationResult("Unsupported country code. Supported codes: +65 (Singapore), +60 (Malaysia), +62 (Indonesia).");
                }
            }
            else
            {
                // Singapore mobile without country code: 8 digits, starts with 8 or 9
                if (!Regex.IsMatch(mobile, @"^[89]\d{7}$"))
                {
                    return new ValidationResult("Mobile number must be 8 digits and start with 8 or 9, or include a valid country code.");
                }
            }

            return ValidationResult.Success;
        }
    }
}
