using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WebApplication1.Model;
using WebApplication1.Services;
using WebApplication1.ViewModels;
using System.Security.Cryptography;
using System.Text;

namespace WebApplication1.Pages
{
    public class LoginModel : PageModel
    {
        private string MaskEmail(string? email)
        {
            if (string.IsNullOrEmpty(email))
            {
                return string.Empty;
            }

            var atIndex = email.IndexOf('@');
            if (atIndex <= 1)
            {
                // Not a typical email format; avoid logging the raw value.
                return "***";
            }

            var localPart = email.Substring(0, atIndex);
            var domainPart = email.Substring(atIndex);

            if (localPart.Length <= 2)
            {
                return new string('*', localPart.Length) + domainPart;
            }

            var visiblePrefix = localPart.Substring(0, 1);
            var maskedMiddle = new string('*', localPart.Length - 2);
            var visibleSuffix = localPart.Substring(localPart.Length - 1, 1);

            return visiblePrefix + maskedMiddle + visibleSuffix + domainPart;
        }

        private string GetEmailHash(string? email)
        {
            if (string.IsNullOrEmpty(email))
            {
                return string.Empty;
            }

            using (var sha256 = SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(email);
                var hashBytes = sha256.ComputeHash(bytes);
                var sb = new StringBuilder(hashBytes.Length * 2);
                foreach (var b in hashBytes)
                {
                    sb.Append(b.ToString("x2"));
                }

                // Return a shortened hash for log readability; still non-reversible.
                return sb.ToString().Substring(0, 8);
            }
        }
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IAuditService _auditService;
        private readonly ISessionService _sessionService;
        private readonly IReCaptchaService _reCaptchaService;
        private readonly IInputSanitizationService _sanitizationService;
        private readonly ILogger<LoginModel> _logger;

        public LoginModel(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            IAuditService auditService,
            ISessionService sessionService,
            IReCaptchaService reCaptchaService,
            IInputSanitizationService sanitizationService,
            ILogger<LoginModel> logger)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _auditService = auditService;
            _sessionService = sessionService;
            _reCaptchaService = reCaptchaService;
            _sanitizationService = sanitizationService;
            _logger = logger;
        }

        private static string RedactEmailForLogging(string email)
        {
            if (string.IsNullOrEmpty(email) || !email.Contains('@'))
            {
                return "redacted";
            }

            var parts = email.Split('@');
            var local = parts[0];
            var domain = parts[1];

            if (local.Length <= 1)
            {
                return $"*@{domain}";
            }

            return $"{local[0]}***@{domain}";
        }

        private static string GetEmailHashForLogging(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                return "unknown";
            }

            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(email);
            var hashBytes = sha256.ComputeHash(bytes);
            var sb = new StringBuilder(hashBytes.Length * 2);
            foreach (var b in hashBytes)
            {
                sb.Append(b.ToString("x2"));
            }

            return sb.ToString();
        }

        [BindProperty]
        public required Login LModel { get; set; }

        public string? ReturnUrl { get; set; }

        [TempData]
        public string? ErrorMessage { get; set; }

        public string? TimeoutMessage { get; set; }

        public void OnGet(string? returnUrl = null, bool timeout = false)
        {
            if (timeout)
            {
                TimeoutMessage = "Your session has expired. Please log in again.";
            }

            if (!string.IsNullOrEmpty(ErrorMessage))
            {
                ModelState.AddModelError(string.Empty, ErrorMessage);
            }

            ReturnUrl = returnUrl ?? Url.Content("~/");
        }

        public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");

            if (ModelState.IsValid)
            {
                // Sanitize email input
                var sanitizedEmail = _sanitizationService.SanitizeInput(LModel.Email);
                var maskedEmail = MaskEmail(sanitizedEmail);

                // Check for potential attacks
                if (_sanitizationService.ContainsPotentialXss(sanitizedEmail))
                {
                    _logger.LogWarning("Potential XSS attack detected in login request.");
                    ModelState.AddModelError(string.Empty, "Invalid email format.");
                    return Page();
                }

                if (_sanitizationService.ContainsPotentialSqlInjection(sanitizedEmail))
                {
                    _logger.LogWarning("Potential SQL injection detected in login request.");
                    ModelState.AddModelError(string.Empty, "Invalid email format.");
                    return Page();
                }

                // Verify reCAPTCHA token
                var recaptchaToken = Request.Form["g-recaptcha-response"].ToString();
                
                _logger.LogInformation("Attempting login. reCAPTCHA token present: {TokenPresent}, Token length: {TokenLength}", 
                    !string.IsNullOrEmpty(recaptchaToken),
                    recaptchaToken?.Length ?? 0);

                var isRecaptchaValid = await _reCaptchaService.VerifyTokenAsync(recaptchaToken, "login");


                if (!isRecaptchaValid)
                {
                    _logger.LogWarning("reCAPTCHA validation failed for login attempt. Token was: {TokenPresent}", 
                        !string.IsNullOrEmpty(recaptchaToken) ? "Present" : "Missing");
                    ModelState.AddModelError(string.Empty, "reCAPTCHA validation failed. Please try again.");
                    return Page();
                }

                _logger.LogInformation("reCAPTCHA validation successful for login attempt");


                var user = await _userManager.FindByEmailAsync(sanitizedEmail);
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                var userAgent = HttpContext.Request.Headers["User-Agent"].ToString();

                var result = await _signInManager.PasswordSignInAsync(
                    sanitizedEmail,
                    LModel.Password,
                    LModel.RememberMe,
                    lockoutOnFailure: true);

                if (result.Succeeded)
                {
                    _logger.LogInformation("User logged in successfully.");
                    
                    if (user != null)
                    {
                        // Check for existing active sessions
                        var activeSessionCount = await _sessionService.GetActiveSessionCountAsync(user.Id);
                        
                        if (activeSessionCount > 0)
                        {
                            _logger.LogWarning("Multiple login detected for user {UserId}. Active sessions: {Count}", 
                                user.Id, activeSessionCount);
                            
                            await _auditService.LogActivityAsync(
                                user.Id,
                                "Multiple Login Detected",
                                $"User has {activeSessionCount} active session(s). New login from {ipAddress}",
                                ipAddress,
                                userAgent);

                            // Option 1: Allow multiple sessions (just log a warning)
                            // Option 2: Invalidate previous sessions (uncomment below)
                            // await _sessionService.InvalidateAllUserSessionsAsync(user.Id);
                            // _logger.LogInformation("Previous sessions invalidated for user {UserId}", user.Id);
                        }

                        // Initialize session by setting a value first
                        await HttpContext.Session.LoadAsync();
                        
                        // Create a new secured session - use ASP.NET Core's session ID or generate new one
                        var sessionId = HttpContext.Session.Id;
                        if (string.IsNullOrEmpty(sessionId))
                        {
                            // Force session creation by setting a dummy value
                            HttpContext.Session.SetString("Init", "true");
                            sessionId = HttpContext.Session.Id;
                        }
                        
                        // If still empty, generate manually
                        if (string.IsNullOrEmpty(sessionId))
                        {
                            sessionId = Guid.NewGuid().ToString();
                        }

                        await _sessionService.CreateSessionAsync(user.Id, sessionId, ipAddress ?? "Unknown", userAgent);
                        
                        // Store session ID and user info in session
                        HttpContext.Session.SetString("SessionId", sessionId);
                        HttpContext.Session.SetString("UserId", user.Id);
                        HttpContext.Session.SetString("Email", user.Email ?? "");
                        HttpContext.Session.SetString("LoginTime", DateTime.UtcNow.ToString("o"));
                        
                        // Ensure session is saved
                        await HttpContext.Session.CommitAsync();

                        await _auditService.LogActivityAsync(
                            user.Id,
                            "Login Success",
                            $"User logged in successfully from {ipAddress}. Session ID: {sessionId}",
                            ipAddress,
                            userAgent);
                        
                        _logger.LogInformation("Session created with ID: {SessionId}", sessionId);
                    }
                    
                    return LocalRedirect(returnUrl);
                }

                if (result.RequiresTwoFactor)
                {
                    _logger.LogInformation("User with ID {UserId} requires 2FA verification", user?.Id);
                    
                    if (user != null)
                    {
                        await _auditService.LogActivityAsync(
                            user.Id,
                            "Login - 2FA Required",
                            $"2FA verification required from {ipAddress}",
                            ipAddress,
                            userAgent);
                    }
                    
                    return RedirectToPage("/Verify2FA", new { returnUrl = returnUrl });
                }

                if (result.IsLockedOut)
                {
                    _logger.LogWarning("User account locked out.");
                    
                    if (user != null)
                    {
                        await _auditService.LogActivityAsync(
                            user.Id,
                            "Login Failed - Locked Out",
                            $"Account locked out due to multiple failed login attempts from {ipAddress}",
                            ipAddress,
                            userAgent);
                    }
                    
                    ModelState.AddModelError(string.Empty, "Account locked out due to multiple failed login attempts. Please try again after 1 minute.");
                    return Page();
                }

                if (result.IsNotAllowed)
                {
                    _logger.LogWarning("User not allowed to sign in");
                    
                    if (user != null)
                    {
                        await _auditService.LogActivityAsync(
                            user.Id,
                            "Login Failed - Not Allowed",
                            $"Login not allowed from {ipAddress}",
                            ipAddress,
                            userAgent);
                    }
                    
                    ModelState.AddModelError(string.Empty, "Login not allowed.");
                    return Page();
                }

                _logger.LogWarning("Invalid login attempt");
                
                if (user != null)
                {
                    await _auditService.LogActivityAsync(
                        user.Id,
                        "Login Failed - Invalid Credentials",
                        $"Invalid login attempt from {ipAddress}",
                        ipAddress,
                        userAgent);
                }
                
                ModelState.AddModelError(string.Empty, "Invalid email or password.");
            }

            return Page();
        }
    }
}
