using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Model;
using WebApplication1.Services;
using WebApplication1.ViewModels;

namespace WebApplication1.Pages
{
    public class ResetPasswordSmsModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IPasswordManagementService _passwordManagement;
        private readonly IAuditService _auditService;
        private readonly IEmailService _emailService;
        private readonly ILogger<ResetPasswordSmsModel> _logger;

        public ResetPasswordSmsModel(
            UserManager<ApplicationUser> userManager,
            IPasswordManagementService passwordManagement,
            IAuditService auditService,
            IEmailService emailService,
            ILogger<ResetPasswordSmsModel> logger)
        {
            _userManager = userManager;
            _passwordManagement = passwordManagement;
            _auditService = auditService;
            _emailService = emailService;
            _logger = logger;
        }

        [BindProperty]
        public required ResetPasswordSms Model { get; set; }

        public string? UserEmail { get; set; }

        public IActionResult OnGet(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                return RedirectToPage("/ForgotPassword");
            }

            UserEmail = email;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Mobile == Model.Mobile);
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Invalid mobile number or reset code.");
                return Page();
            }

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var userAgent = HttpContext.Request.Headers["User-Agent"].ToString();

            // Verify the reset code from session
            var storedCode = HttpContext.Session.GetString($"PasswordResetCode_{user.Id}");
            var expiryString = HttpContext.Session.GetString($"PasswordResetCodeExpiry_{user.Id}");

            if (string.IsNullOrEmpty(storedCode) || string.IsNullOrEmpty(expiryString))
            {
                ModelState.AddModelError(string.Empty, "Reset code has expired. Please request a new one.");
                await _auditService.LogActivityAsync(
                    user.Id,
                    "Password Reset Failed - Expired Code",
                    $"Attempt from {ipAddress}",
                    ipAddress,
                    userAgent);
                return Page();
            }

            var expiry = DateTime.Parse(expiryString);
            if (DateTime.UtcNow > expiry)
            {
                HttpContext.Session.Remove($"PasswordResetCode_{user.Id}");
                HttpContext.Session.Remove($"PasswordResetCodeExpiry_{user.Id}");
                ModelState.AddModelError(string.Empty, "Reset code has expired. Please request a new one.");
                await _auditService.LogActivityAsync(
                    user.Id,
                    "Password Reset Failed - Expired Code",
                    $"Attempt from {ipAddress}",
                    ipAddress,
                    userAgent);
                return Page();
            }

            if (storedCode != Model.ResetCode)
            {
                ModelState.AddModelError(string.Empty, "Invalid reset code.");
                await _auditService.LogActivityAsync(
                    user.Id,
                    "Password Reset Failed - Invalid Code",
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

            // Clear the reset code from session
            HttpContext.Session.Remove($"PasswordResetCode_{user.Id}");
            HttpContext.Session.Remove($"PasswordResetCodeExpiry_{user.Id}");

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
                "Password Reset Success - SMS",
                $"Password reset via SMS from {ipAddress}",
                ipAddress,
                userAgent);

            _logger.LogInformation("User {UserId} reset password via SMS successfully", user.Id);

            TempData["SuccessMessage"] = "Your password has been reset successfully. Please log in with your new password.";
            return RedirectToPage("/Login");
        }
    }
}
