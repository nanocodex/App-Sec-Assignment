using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WebApplication1.Model;
using WebApplication1.Services;
using WebApplication1.ViewModels;

namespace WebApplication1.Pages
{
    [Authorize]
    public class ChangePasswordModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IPasswordManagementService _passwordManagement;
        private readonly IAuditService _auditService;
        private readonly IEmailService _emailService;
        private readonly ILogger<ChangePasswordModel> _logger;

        public ChangePasswordModel(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IPasswordManagementService passwordManagement,
            IAuditService auditService,
            IEmailService emailService,
            ILogger<ChangePasswordModel> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _passwordManagement = passwordManagement;
            _auditService = auditService;
            _emailService = emailService;
            _logger = logger;
        }

        [BindProperty]
        public required ChangePassword Model { get; set; }

        public bool MustChangePassword { get; set; }
        public string? InfoMessage { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToPage("/Login");
            }

            MustChangePassword = await _passwordManagement.MustChangePasswordAsync(user.Id);

            if (MustChangePassword)
            {
                InfoMessage = "Your password has expired. You must change it to continue.";
            }
            else if (user.LastPasswordChangeDate.HasValue)
            {
                var daysSinceChange = (DateTime.UtcNow - user.LastPasswordChangeDate.Value).Days;
                var maxAgeDays = 90; // From configuration
                var daysRemaining = maxAgeDays - daysSinceChange;

                if (daysRemaining <= 7 && daysRemaining > 0)
                {
                    InfoMessage = $"Your password will expire in {daysRemaining} day(s). Please change it soon.";
                }
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToPage("/Login");
            }

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var userAgent = HttpContext.Request.Headers["User-Agent"].ToString();

            // Check if user can change password (minimum age policy)
            var (canChange, message) = await _passwordManagement.CanChangePasswordAsync(user.Id);
            if (!canChange)
            {
                ModelState.AddModelError(string.Empty, message);
                await _auditService.LogActivityAsync(
                    user.Id,
                    "Password Change Failed - Too Soon",
                    message,
                    ipAddress,
                    userAgent);
                return Page();
            }

            // Verify current password
            var isCurrentPasswordCorrect = await _userManager.CheckPasswordAsync(user, Model.CurrentPassword);
            if (!isCurrentPasswordCorrect)
            {
                ModelState.AddModelError(string.Empty, "Current password is incorrect.");
                await _auditService.LogActivityAsync(
                    user.Id,
                    "Password Change Failed - Incorrect Current Password",
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
                    "Password Change Failed - Password Reused",
                    $"Attempt from {ipAddress}",
                    ipAddress,
                    userAgent);
                return Page();
            }

            // Change password
            var changePasswordResult = await _userManager.ChangePasswordAsync(user, Model.CurrentPassword, Model.NewPassword);
            if (!changePasswordResult.Succeeded)
            {
                foreach (var error in changePasswordResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return Page();
            }

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
                "Password Changed",
                $"Password changed successfully from {ipAddress}",
                ipAddress,
                userAgent);

            _logger.LogInformation("User {UserId} changed password successfully", user.Id);

            // Sign in the user again to refresh the security stamp
            await _signInManager.RefreshSignInAsync(user);

            TempData["SuccessMessage"] = "Your password has been changed successfully.";
            return RedirectToPage("/Index");
        }
    }
}
