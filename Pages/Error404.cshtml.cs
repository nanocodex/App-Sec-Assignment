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
            _logger.LogWarning("404 Error - Page not found: {Path}", HttpContext.Request.Path);
        }
    }
}
