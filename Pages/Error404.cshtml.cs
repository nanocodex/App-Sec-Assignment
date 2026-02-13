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
            var rawPath = HttpContext.Request.Path.ToString();
            var sanitizedPath = rawPath
                .Replace(Environment.NewLine, string.Empty)
                .Replace("\n", string.Empty)
                .Replace("\r", string.Empty);

            _logger.LogWarning("404 Error - Page not found: {Path}", sanitizedPath);
        }
    }
}
