using Microsoft.AspNetCore.Mvc.RazorPages;

namespace WebApplication1.Pages
{
    public class Error403Model : PageModel
    {
        private readonly ILogger<Error403Model> _logger;

        public Error403Model(ILogger<Error403Model> logger)
        {
            _logger = logger;
        }

        public void OnGet()
        {
            // Get the original path from the request features
            var statusCodeFeature = HttpContext.Features.Get<Microsoft.AspNetCore.Diagnostics.IStatusCodeReExecuteFeature>();
            
            if (statusCodeFeature != null)
            {
                // This is a legitimate 403 redirect - log the original path
                var originalPath = statusCodeFeature.OriginalPath;
                _logger.LogWarning("403 Error - Access denied: {Path}", originalPath);
            }
            // If statusCodeFeature is null, user navigated directly to /Error403
            // Don't log anything in this case
        }
    }
}
