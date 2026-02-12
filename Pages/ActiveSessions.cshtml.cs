using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WebApplication1.Model;
using WebApplication1.Services;

namespace WebApplication1.Pages
{
    [Authorize]
    public class ActiveSessionsModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ISessionService _sessionService;
        private readonly IAuditService _auditService;
        private readonly ILogger<ActiveSessionsModel> _logger;

        public ActiveSessionsModel(
            UserManager<ApplicationUser> userManager, 
            ISessionService sessionService,
            IAuditService auditService,
            ILogger<ActiveSessionsModel> logger)
        {
            _userManager = userManager;
            _sessionService = sessionService;
            _auditService = auditService;
            _logger = logger;
        }

        public List<UserSession> ActiveSessions { get; set; } = new();
        public string? CurrentSessionId { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToPage("/Login");
            }

            // Ensure session is loaded
            await HttpContext.Session.LoadAsync();
            CurrentSessionId = HttpContext.Session.GetString("SessionId");
            
            _logger.LogInformation("Current Session ID: {SessionId}", CurrentSessionId ?? "NULL");
            
            ActiveSessions = await _sessionService.GetActiveSessionsAsync(user.Id);
            
            _logger.LogInformation("Found {Count} active sessions", ActiveSessions.Count);

            return Page();
        }

        public async Task<IActionResult> OnPostTerminateSessionAsync(int sessionId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToPage("/Login");
            }

            var sessions = await _sessionService.GetActiveSessionsAsync(user.Id);
            var sessionToTerminate = sessions.FirstOrDefault(s => s.Id == sessionId);

            if (sessionToTerminate != null)
            {
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                var userAgent = HttpContext.Request.Headers["User-Agent"].ToString();
                
                await _sessionService.InvalidateSessionAsync(user.Id, sessionToTerminate.SessionId);
                
                await _auditService.LogActivityAsync(
                    user.Id,
                    "Session Terminated",
                    $"User terminated session {sessionToTerminate.SessionId} from {sessionToTerminate.IpAddress}",
                    ipAddress,
                    userAgent);
                
                _logger.LogInformation("Session {SessionId} terminated by user {UserId}", sessionToTerminate.SessionId, user.Id);
                
                TempData["SuccessMessage"] = "Session terminated successfully.";
            }
            else
            {
                _logger.LogWarning("Session ID {SessionId} not found for termination", sessionId);
                TempData["ErrorMessage"] = "Session not found.";
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostTerminateAllOtherSessionsAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToPage("/Login");
            }

            // Ensure session is loaded
            await HttpContext.Session.LoadAsync();
            var currentSessionId = HttpContext.Session.GetString("SessionId");

            if (string.IsNullOrEmpty(currentSessionId))
            {
                _logger.LogWarning("Cannot terminate other sessions - current session ID not found for user {UserId}", user.Id);
                TempData["ErrorMessage"] = "Unable to identify current session. Please try logging out and logging back in.";
                return RedirectToPage();
            }

            // Get count of sessions to be terminated (for feedback)
            var activeSessions = await _sessionService.GetActiveSessionsAsync(user.Id);
            var otherSessionsCount = activeSessions.Count(s => s.SessionId != currentSessionId);

            if (otherSessionsCount == 0)
            {
                TempData["InfoMessage"] = "No other sessions to terminate. You only have one active session.";
                return RedirectToPage();
            }

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var userAgent = HttpContext.Request.Headers["User-Agent"].ToString();

            await _sessionService.InvalidateAllUserSessionsExceptCurrentAsync(user.Id, currentSessionId);

            await _auditService.LogActivityAsync(
                user.Id,
                "All Other Sessions Terminated",
                $"User terminated {otherSessionsCount} other session(s), keeping current session {currentSessionId}",
                ipAddress,
                userAgent);

            _logger.LogInformation("User {UserId} terminated {Count} other sessions", user.Id, otherSessionsCount);

            TempData["SuccessMessage"] = $"Successfully terminated {otherSessionsCount} other session(s). Only your current session remains active.";

            return RedirectToPage();
        }
    }
}
