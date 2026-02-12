using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using WebApplication1.Model;
using WebApplication1.Services;

namespace WebApplication1.Pages
{
    [AllowAnonymous]
    public class Verify2FAModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ITwoFactorService _twoFactorService;
        private readonly IAuditService _auditService;
        private readonly ISessionService _sessionService;
        private readonly ILogger<Verify2FAModel> _logger;

        public Verify2FAModel(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            ITwoFactorService twoFactorService,
            IAuditService auditService,
            ISessionService sessionService,
            ILogger<Verify2FAModel> logger)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _twoFactorService = twoFactorService;
            _auditService = auditService;
            _sessionService = sessionService;
            _logger = logger;
        }

        [BindProperty]
        [Display(Name = "Authentication Code")]
        public string? TwoFactorCode { get; set; }

        [BindProperty]
        [Display(Name = "Recovery Code")]
        public string? RecoveryCode { get; set; }

        [BindProperty]
        [Display(Name = "Remember this device")]
        public bool RememberMachine { get; set; }

        [BindProperty]
        public string? ReturnUrl { get; set; }

        public bool UseRecoveryCode { get; set; }

        public async Task<IActionResult> OnGetAsync(string? returnUrl = null)
        {
            // Ensure the user has gone through the username & password screen first
            var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
            
            if (user == null)
            {
                _logger.LogWarning("2FA verification accessed without valid 2FA context");
                return RedirectToPage("/Login");
            }

            ReturnUrl = returnUrl ?? Url.Content("~/");
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");

            var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
            if (user == null)
            {
                _logger.LogWarning("2FA verification attempted without valid 2FA context");
                return RedirectToPage("/Login");
            }

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var userAgent = HttpContext.Request.Headers["User-Agent"].ToString();

            if (UseRecoveryCode)
            {
                return await HandleRecoveryCodeLogin(user, returnUrl, ipAddress, userAgent);
            }
            else
            {
                return await HandleAuthenticatorCodeLogin(user, returnUrl, ipAddress, userAgent);
            }
        }

        public IActionResult OnPostUseRecoveryCode()
        {
            UseRecoveryCode = true;
            return Page();
        }

        public IActionResult OnPostUseAuthenticatorCode()
        {
            UseRecoveryCode = false;
            return Page();
        }

        private async Task<IActionResult> HandleAuthenticatorCodeLogin(
            ApplicationUser user, string returnUrl, string? ipAddress, string userAgent)
        {
            if (string.IsNullOrEmpty(TwoFactorCode))
            {
                ModelState.AddModelError(nameof(TwoFactorCode), "Authentication code is required.");
                return Page();
            }

            // Remove any spaces or dashes
            var code = TwoFactorCode.Replace(" ", string.Empty).Replace("-", string.Empty);

            if (!System.Text.RegularExpressions.Regex.IsMatch(code, @"^\d{6}$"))
            {
                ModelState.AddModelError(nameof(TwoFactorCode), "Authentication code must be 6 digits.");
                return Page();
            }

            // Get the authenticator key
            var authenticatorKey = await _userManager.GetAuthenticationTokenAsync(
                user,
                "[AspNetUserStore]",
                "AuthenticatorKey");
            
            if (string.IsNullOrEmpty(authenticatorKey))
            {
                _logger.LogError("Authenticator key not found for user {UserId}", user.Id);
                ModelState.AddModelError(string.Empty, "Two-factor authentication is not properly configured.");
                return Page();
            }

            // Verify the code
            var isValid = _twoFactorService.VerifyTotpCode(authenticatorKey, code);

            if (!isValid)
            {
                _logger.LogWarning("Invalid 2FA code for user {UserId} from {IpAddress}", user.Id, ipAddress);
                
                await _auditService.LogActivityAsync(
                    user.Id,
                    "2FA Login Failed - Invalid Code",
                    $"Invalid 2FA code entered from {ipAddress}",
                    ipAddress,
                    userAgent);

                ModelState.AddModelError(string.Empty, "Invalid authentication code.");
                return Page();
            }

            // Sign in the user
            await _signInManager.SignInAsync(user, isPersistent: false);

            if (RememberMachine)
            {
                await _signInManager.RememberTwoFactorClientAsync(user);
            }

            // Create session
            await HttpContext.Session.LoadAsync();
            var sessionId = HttpContext.Session.Id;
            
            if (string.IsNullOrEmpty(sessionId))
            {
                HttpContext.Session.SetString("Init", "true");
                sessionId = HttpContext.Session.Id;
            }
            
            if (string.IsNullOrEmpty(sessionId))
            {
                sessionId = Guid.NewGuid().ToString();
            }

            await _sessionService.CreateSessionAsync(user.Id, sessionId, ipAddress ?? "Unknown", userAgent);
            
            HttpContext.Session.SetString("SessionId", sessionId);
            HttpContext.Session.SetString("UserId", user.Id);
            HttpContext.Session.SetString("Email", user.Email ?? "");
            HttpContext.Session.SetString("LoginTime", DateTime.UtcNow.ToString("o"));
            
            await HttpContext.Session.CommitAsync();

            await _auditService.LogActivityAsync(
                user.Id,
                "2FA Login Success",
                $"User logged in successfully with 2FA from {ipAddress}",
                ipAddress,
                userAgent);

            _logger.LogInformation("User {UserId} logged in with 2FA", user.Id);

            return LocalRedirect(returnUrl);
        }

        private async Task<IActionResult> HandleRecoveryCodeLogin(
            ApplicationUser user, string returnUrl, string? ipAddress, string userAgent)
        {
            if (string.IsNullOrEmpty(RecoveryCode))
            {
                ModelState.AddModelError(nameof(RecoveryCode), "Recovery code is required.");
                UseRecoveryCode = true;
                return Page();
            }

            var encryptedCodes = user.TwoFactorRecoveryCodes;
            
            if (string.IsNullOrEmpty(encryptedCodes))
            {
                _logger.LogWarning("No recovery codes found for user {UserId}", user.Id);
                ModelState.AddModelError(string.Empty, "No recovery codes available.");
                UseRecoveryCode = true;
                return Page();
            }

            // Verify and consume the recovery code
            var isValid = _twoFactorService.VerifyAndConsumeRecoveryCode(
                encryptedCodes, 
                RecoveryCode, 
                out string updatedCodes);

            if (!isValid)
            {
                _logger.LogWarning("Invalid recovery code for user {UserId} from {IpAddress}", user.Id, ipAddress);
                
                await _auditService.LogActivityAsync(
                    user.Id,
                    "2FA Login Failed - Invalid Recovery Code",
                    $"Invalid recovery code entered from {ipAddress}",
                    ipAddress,
                    userAgent);

                ModelState.AddModelError(string.Empty, "Invalid recovery code.");
                UseRecoveryCode = true;
                return Page();
            }

            // Update the user's recovery codes (with the used code removed)
            user.TwoFactorRecoveryCodes = updatedCodes;
            await _userManager.UpdateAsync(user);

            // Sign in the user
            await _signInManager.SignInAsync(user, isPersistent: false);

            // Create session
            await HttpContext.Session.LoadAsync();
            var sessionId = HttpContext.Session.Id;
            
            if (string.IsNullOrEmpty(sessionId))
            {
                HttpContext.Session.SetString("Init", "true");
                sessionId = HttpContext.Session.Id;
            }
            
            if (string.IsNullOrEmpty(sessionId))
            {
                sessionId = Guid.NewGuid().ToString();
            }

            await _sessionService.CreateSessionAsync(user.Id, sessionId, ipAddress ?? "Unknown", userAgent);
            
            HttpContext.Session.SetString("SessionId", sessionId);
            HttpContext.Session.SetString("UserId", user.Id);
            HttpContext.Session.SetString("Email", user.Email ?? "");
            HttpContext.Session.SetString("LoginTime", DateTime.UtcNow.ToString("o"));
            
            await HttpContext.Session.CommitAsync();

            await _auditService.LogActivityAsync(
                user.Id,
                "2FA Login Success - Recovery Code",
                $"User logged in with recovery code from {ipAddress}. Remaining codes: {_twoFactorService.DecryptRecoveryCodes(updatedCodes).Count}",
                ipAddress,
                userAgent);

            _logger.LogInformation("User {UserId} logged in with recovery code", user.Id);

            // Warn user if running low on recovery codes
            var remainingCodes = _twoFactorService.DecryptRecoveryCodes(updatedCodes).Count;
            if (remainingCodes <= 3)
            {
                TempData["WarningMessage"] = $"Warning: You only have {remainingCodes} recovery code(s) left. Consider generating new ones.";
            }

            return LocalRedirect(returnUrl);
        }
    }
}
