using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WebApplication1.Model;
using WebApplication1.Services;

namespace WebApplication1.Pages
{
    public class LogoutModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IAuditService _auditService;
        private readonly ISessionService _sessionService;
        private readonly ILogger<LogoutModel> _logger;

        public LogoutModel(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            IAuditService auditService,
            ISessionService sessionService,
            ILogger<LogoutModel> logger)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _auditService = auditService;
            _sessionService = sessionService;
            _logger = logger;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            return await PerformLogoutAsync();
        }

        public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
        {
            return await PerformLogoutAsync();
        }

        private async Task<IActionResult> PerformLogoutAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var userAgent = HttpContext.Request.Headers["User-Agent"].ToString();

            if (user != null)
            {
                // Invalidate the current session
                var sessionId = HttpContext.Session.GetString("SessionId");
                if (!string.IsNullOrEmpty(sessionId))
                {
                    await _sessionService.InvalidateSessionAsync(user.Id, sessionId);
                }

                await _auditService.LogActivityAsync(
                    user.Id,
                    "Logout",
                    $"User logged out from {ipAddress}",
                    ipAddress,
                    userAgent);
            }

            await _signInManager.SignOutAsync();
            _logger.LogInformation("User logged out.");

            // Clear session
            HttpContext.Session.Clear();

            // Redirect to login page
            return RedirectToPage("/Login");
        }
    }
}
