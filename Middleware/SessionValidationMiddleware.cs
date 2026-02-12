using Microsoft.AspNetCore.Identity;
using WebApplication1.Model;
using WebApplication1.Services;

namespace WebApplication1.Middleware
{
    public class SessionValidationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<SessionValidationMiddleware> _logger;

        public SessionValidationMiddleware(RequestDelegate next, ILogger<SessionValidationMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, 
            UserManager<ApplicationUser> userManager, 
            SignInManager<ApplicationUser> signInManager,
            ISessionService sessionService)
        {
            // Skip validation for login, logout, error pages, and static files
            var path = context.Request.Path.Value?.ToLower() ?? "";
            if (path.Contains("/login") || 
                path.Contains("/logout") || 
                path.Contains("/register") || 
                path.Contains("/error") ||
                path.Contains("/lib/") ||
                path.Contains("/css/") ||
                path.Contains("/js/") ||
                path.Contains("/favicon"))
            {
                await _next(context);
                return;
            }

            // Check if user is authenticated
            if (context.User.Identity?.IsAuthenticated == true)
            {
                var user = await userManager.GetUserAsync(context.User);
                if (user != null)
                {
                    var sessionId = context.Session.GetString("SessionId");

                    if (string.IsNullOrEmpty(sessionId))
                    {
                        // No session ID found - session may have expired
                        _logger.LogWarning("No session ID found for authenticated user {UserId}", user.Id);
                        await signInManager.SignOutAsync();
                        context.Response.Redirect("/Login?timeout=true");
                        return;
                    }

                    // Validate the session
                    var isValid = await sessionService.ValidateSessionAsync(user.Id, sessionId);
                    if (!isValid)
                    {
                        _logger.LogWarning("Invalid or expired session for user {UserId}", user.Id);
                        await signInManager.SignOutAsync();
                        context.Session.Clear();
                        context.Response.Redirect("/Login?timeout=true");
                        return;
                    }

                    // Update session activity
                    await sessionService.UpdateSessionActivityAsync(user.Id, sessionId);
                }
            }

            await _next(context);
        }
    }

    public static class SessionValidationMiddlewareExtensions
    {
        public static IApplicationBuilder UseSessionValidation(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<SessionValidationMiddleware>();
        }
    }
}
