using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WebApplication1.Model;
using WebApplication1.Services;
using WebApplication1.ViewModels;

namespace WebApplication1.Pages
{
    public class ResetPasswordModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IPasswordManagementService _passwordManagement;
        private readonly IAuditService _auditService;
        private readonly IEmailService _emailService;
        private readonly ILogger<ResetPasswordModel> _logger;

        public ResetPasswordModel(
            UserManager<ApplicationUser> userManager,
            IPasswordManagementService passwordManagement,
            IAuditService auditService,
            IEmailService emailService,
            ILogger<ResetPasswordModel> logger)
        {
            _userManager = userManager;
            _passwordManagement = passwordManagement;
            _auditService = auditService;
            _emailService = emailService;
            _logger = logger;
        }

        [BindProperty]
        public required ResetPassword Model { get; set; }

        public string? ErrorMessage { get; set; }

        public async Task<IActionResult> OnGetAsync(string userId, string token)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(token))
            {
                ErrorMessage = "Invalid password reset link.";
                return Page();
            }

            // Validate the token
            var isValid = await _passwordManagement.ValidatePasswordResetTokenAsync(userId, token);
            if (!isValid)
            {
                ErrorMessage = "This password reset link is invalid or has expired. Please request a new one.";
                return Page();
            }

            Model = new ResetPassword
            {
                UserId = userId,
                Token = token,
                NewPassword = string.Empty,
                ConfirmPassword = string.Empty
            };

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var user = await _userManager.FindByIdAsync(Model.UserId);
            if (user == null)
            {
                ErrorMessage = "User not found.";
                return Page();
            }

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var userAgent = HttpContext.Request.Headers["User-Agent"].ToString();

            // Validate the token again
            var isValid = await _passwordManagement.ValidatePasswordResetTokenAsync(Model.UserId, Model.Token);
            if (!isValid)
            {
                ErrorMessage = "This password reset link is invalid or has expired.";
                await _auditService.LogActivityAsync(
                    user.Id,
                    "Password Reset Failed - Invalid Token",
                    $"Attempt from {ipAddress}",
                    ipAddress,
                    userAgent);
                return Page();
            }

            // Check password history
            var isPasswordReused = !await _passwordManagement.CheckPasswordHistoryAsync(user.Id, Model.NewPassword);
            if (isPasswordReused)
            {
                ModelState.AddModelError(string.Empty, "You cannot reuse any of your last 2 passwords. Please choose a different password.");
                await _auditService.LogActivityAsync(
                    user.Id,
                    "Password Reset Failed - Password Reused",
                    $"Attempt from {ipAddress}",
                    ipAddress,
                    userAgent);
                return Page();
            }

            // Remove the user's password and add the new one
            await _userManager.RemovePasswordAsync(user);
            var addPasswordResult = await _userManager.AddPasswordAsync(user, Model.NewPassword);

            if (!addPasswordResult.Succeeded)
            {
                foreach (var error in addPasswordResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return Page();
            }

            // Mark token as used
            await _passwordManagement.UsePasswordResetTokenAsync(Model.UserId, Model.Token);

            // Add to password history
            var newPasswordHash = _userManager.PasswordHasher.HashPassword(user, Model.NewPassword);
            await _passwordManagement.AddPasswordToHistoryAsync(user.Id, newPasswordHash);

            // Update password change date
            await _passwordManagement.UpdatePasswordChangeDateAsync(user.Id);

            // Send notification email
            await _emailService.SendPasswordChangedNotificationAsync(user.Email!, $"{user.FirstName} {user.LastName}");

            // Log the activity
            await _auditService.LogActivityAsync(
                user.Id,
                "Password Reset Success",
                $"Password reset successfully from {ipAddress}",
                ipAddress,
                userAgent);

            _logger.LogInformation("User {UserId} reset password successfully", user.Id);

            TempData["SuccessMessage"] = "Your password has been reset successfully. Please log in with your new password.";
            return RedirectToPage("/Login");
        }
    }
}
