using Microsoft.AspNetCore.Mvc.RazorPages;

namespace WebApplication1.Pages
{
    public class Error404Model : PageModel
    {
        private readonly ILogger<Error404Model> _logger;

        public Error404Model(ILogger<Error404Model> logger)
        {
            _logger = logger;
        }

        public void OnGet()
        {
            // Get the original path from the request features
            var statusCodeFeature = HttpContext.Features.Get<Microsoft.AspNetCore.Diagnostics.IStatusCodeReExecuteFeature>();
            
            if (statusCodeFeature != null)
            {
                // This is a legitimate 404 redirect - log the original path
                var originalPath = statusCodeFeature.OriginalPath;
                _logger.LogWarning("404 Error - Page not found: {Path}", originalPath);
            }
            // If statusCodeFeature is null, user navigated directly to /Error404
            // Don't log anything in this case
        }
    }
}
