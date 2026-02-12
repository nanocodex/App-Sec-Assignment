using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WebApplication1.Model;
using WebApplication1.Services;

namespace WebApplication1.Pages
{
    [Authorize]
    public class Manage2FAModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ITwoFactorService _twoFactorService;
        private readonly IAuditService _auditService;
        private readonly ILogger<Manage2FAModel> _logger;

        public Manage2FAModel(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ITwoFactorService twoFactorService,
            IAuditService auditService,
            ILogger<Manage2FAModel> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _twoFactorService = twoFactorService;
            _auditService = auditService;
            _logger = logger;
        }

        public bool Is2FAEnabled { get; set; }
        public int RemainingRecoveryCodes { get; set; }
        public List<string>? NewRecoveryCodes { get; set; }
        
        [TempData]
        public string? StatusMessage { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound("Unable to load user.");
            }

            await LoadUserData(user);

            return Page();
        }

        public async Task<IActionResult> OnPostDisable2FAAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound("Unable to load user.");
            }

            if (!user.IsTwoFactorEnabled)
            {
                StatusMessage = "Two-factor authentication is already disabled.";
                await LoadUserData(user);
                return Page();
            }

            // Disable 2FA
            await _userManager.SetTwoFactorEnabledAsync(user, false);
            await _userManager.RemoveAuthenticationTokenAsync(
                user,
                "[AspNetUserStore]",
                "AuthenticatorKey");
            
            user.IsTwoFactorEnabled = false;
            user.TwoFactorRecoveryCodes = null;
            
            await _userManager.UpdateAsync(user);

            // Log the activity
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var userAgent = HttpContext.Request.Headers["User-Agent"].ToString();
            
            await _auditService.LogActivityAsync(
                user.Id,
                "2FA Disabled",
                $"Two-factor authentication disabled from {ipAddress}",
                ipAddress,
                userAgent);

            _logger.LogInformation("Two-factor authentication disabled for user {UserId}", user.Id);

            // Sign out and require re-login for security
            await _signInManager.SignOutAsync();
            
            StatusMessage = "Two-factor authentication has been disabled. Please log in again.";
            
            return RedirectToPage("/Login");
        }

        public async Task<IActionResult> OnPostGenerateRecoveryCodesAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound("Unable to load user.");
            }

            if (!user.IsTwoFactorEnabled)
            {
                StatusMessage = "Two-factor authentication must be enabled before generating recovery codes.";
                await LoadUserData(user);
                return Page();
            }

            // Generate new recovery codes
            NewRecoveryCodes = _twoFactorService.GenerateRecoveryCodes(10);
            user.TwoFactorRecoveryCodes = _twoFactorService.EncryptRecoveryCodes(NewRecoveryCodes);
            
            await _userManager.UpdateAsync(user);

            // Log the activity
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var userAgent = HttpContext.Request.Headers["User-Agent"].ToString();
            
            await _auditService.LogActivityAsync(
                user.Id,
                "2FA Recovery Codes Regenerated",
                $"New recovery codes generated from {ipAddress}",
                ipAddress,
                userAgent);

            _logger.LogInformation("New recovery codes generated for user {UserId}", user.Id);

            StatusMessage = "New recovery codes have been generated. Your old recovery codes are no longer valid.";
            
            await LoadUserData(user);

            return Page();
        }

        private async Task LoadUserData(ApplicationUser user)
        {
            Is2FAEnabled = user.IsTwoFactorEnabled;
            
            if (Is2FAEnabled && !string.IsNullOrEmpty(user.TwoFactorRecoveryCodes))
            {
                var codes = _twoFactorService.DecryptRecoveryCodes(user.TwoFactorRecoveryCodes);
                RemainingRecoveryCodes = codes.Count;
            }
            else
            {
                RemainingRecoveryCodes = 0;
            }
        }
    }
}
