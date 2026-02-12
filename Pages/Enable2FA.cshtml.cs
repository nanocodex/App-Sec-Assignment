using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using WebApplication1.Model;
using WebApplication1.Services;

namespace WebApplication1.Pages
{
    [Authorize]
    public class Enable2FAModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ITwoFactorService _twoFactorService;
        private readonly IAuditService _auditService;
        private readonly ILogger<Enable2FAModel> _logger;

        public Enable2FAModel(
            UserManager<ApplicationUser> userManager,
            ITwoFactorService twoFactorService,
            IAuditService auditService,
            ILogger<Enable2FAModel> logger)
        {
            _userManager = userManager;
            _twoFactorService = twoFactorService;
            _auditService = auditService;
            _logger = logger;
        }

        [BindProperty]
        [Required(ErrorMessage = "Verification code is required")]
        [RegularExpression(@"^\d{6}$", ErrorMessage = "Verification code must be 6 digits")]
        public string? VerificationCode { get; set; }

        [BindProperty]
        public string? SecretKey { get; set; }

        public string? QrCodeImage { get; set; }
        public bool ShowQrCode { get; set; }
        public List<string>? RecoveryCodes { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound("Unable to load user.");
            }

            // Check if 2FA is already enabled
            if (user.IsTwoFactorEnabled)
            {
                TempData["ErrorMessage"] = "Two-factor authentication is already enabled.";
                return RedirectToPage("/Manage2FA");
            }

            // Generate new secret key
            SecretKey = _twoFactorService.GenerateSecretKey();
            
            // Generate QR code
            var qrCodeUri = _twoFactorService.GenerateQrCodeUri(user.Email!, SecretKey);
            QrCodeImage = _twoFactorService.GenerateQrCodeImage(qrCodeUri);
            
            ShowQrCode = true;

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                // Regenerate QR code for display
                var user = await _userManager.GetUserAsync(User);
                if (user != null && !string.IsNullOrEmpty(SecretKey))
                {
                    var qrCodeUri = _twoFactorService.GenerateQrCodeUri(user.Email!, SecretKey);
                    QrCodeImage = _twoFactorService.GenerateQrCodeImage(qrCodeUri);
                    ShowQrCode = true;
                }
                return Page();
            }

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return NotFound("Unable to load user.");
            }

            // Verify the TOTP code
            if (string.IsNullOrEmpty(SecretKey) || string.IsNullOrEmpty(VerificationCode))
            {
                ModelState.AddModelError(string.Empty, "Invalid verification attempt.");
                
                if (!string.IsNullOrEmpty(SecretKey))
                {
                    var qrCodeUri = _twoFactorService.GenerateQrCodeUri(currentUser.Email!, SecretKey);
                    QrCodeImage = _twoFactorService.GenerateQrCodeImage(qrCodeUri);
                    ShowQrCode = true;
                }
                
                return Page();
            }

            var isValid = _twoFactorService.VerifyTotpCode(SecretKey, VerificationCode);
            
            if (!isValid)
            {
                _logger.LogWarning("Invalid 2FA verification code for user {UserId}", currentUser.Id);
                ModelState.AddModelError(string.Empty, "Invalid verification code. Please try again.");
                
                var qrCodeUri = _twoFactorService.GenerateQrCodeUri(currentUser.Email!, SecretKey);
                QrCodeImage = _twoFactorService.GenerateQrCodeImage(qrCodeUri);
                ShowQrCode = true;
                
                return Page();
            }

            // Enable 2FA for the user - store the authenticator key as a user token
            await _userManager.SetAuthenticationTokenAsync(
                currentUser,
                "[AspNetUserStore]",
                "AuthenticatorKey",
                SecretKey);
            
            await _userManager.SetTwoFactorEnabledAsync(currentUser, true);
            
            // Update custom property
            currentUser.IsTwoFactorEnabled = true;

            // Generate and store recovery codes
            RecoveryCodes = _twoFactorService.GenerateRecoveryCodes(10);
            currentUser.TwoFactorRecoveryCodes = _twoFactorService.EncryptRecoveryCodes(RecoveryCodes);
            
            await _userManager.UpdateAsync(currentUser);

            // Log the activity
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var userAgent = HttpContext.Request.Headers["User-Agent"].ToString();
            
            await _auditService.LogActivityAsync(
                currentUser.Id,
                "2FA Enabled",
                $"Two-factor authentication enabled from {ipAddress}",
                ipAddress,
                userAgent);

            _logger.LogInformation("Two-factor authentication enabled for user {UserId}", currentUser.Id);

            ShowQrCode = false;

            return Page();
        }
    }
}
