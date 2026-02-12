using Microsoft.AspNetCore.Identity;
using WebApplication1.Model;

namespace WebApplication1.Services
{
    public class CustomPasswordValidator : IPasswordValidator<ApplicationUser>
    {
        public Task<IdentityResult> ValidateAsync(UserManager<ApplicationUser> manager, ApplicationUser user, string? password)
        {
            List<IdentityError> errors = new List<IdentityError>();

            // Check if password is null or empty
            if (string.IsNullOrEmpty(password))
            {
                errors.Add(new IdentityError
                {
                    Code = "PasswordRequired",
                    Description = "Password is required."
                });
                return Task.FromResult(IdentityResult.Failed(errors.ToArray()));
            }

            // Minimum length check (12 characters)
            if (password.Length < 12)
            {
                errors.Add(new IdentityError
                {
                    Code = "PasswordTooShort",
                    Description = "Password must be at least 12 characters long."
                });
            }

            // Check for lowercase letter
            if (!password.Any(char.IsLower))
            {
                errors.Add(new IdentityError
                {
                    Code = "PasswordRequiresLower",
                    Description = "Password must contain at least one lowercase letter (a-z)."
                });
            }

            // Check for uppercase letter
            if (!password.Any(char.IsUpper))
            {
                errors.Add(new IdentityError
                {
                    Code = "PasswordRequiresUpper",
                    Description = "Password must contain at least one uppercase letter (A-Z)."
                });
            }

            // Check for digit
            if (!password.Any(char.IsDigit))
            {
                errors.Add(new IdentityError
                {
                    Code = "PasswordRequiresDigit",
                    Description = "Password must contain at least one number (0-9)."
                });
            }

            // Check for special character
            string specialChars = "!@#$%^&*()_+-=[]{}|;:'\",.<>?/";
            if (!password.Any(c => specialChars.Contains(c)))
            {
                errors.Add(new IdentityError
                {
                    Code = "PasswordRequiresNonAlphanumeric",
                    Description = "Password must contain at least one special character (!@#$%^&*()_+-=[]{}|;:'\",.<>?/)."
                });
            }

            // If there are errors, return them; otherwise return success
            if (errors.Count > 0)
            {
                return Task.FromResult(IdentityResult.Failed(errors.ToArray()));
            }

            return Task.FromResult(IdentityResult.Success);
        }
    }
}
