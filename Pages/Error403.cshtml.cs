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
            var safePath = HttpContext.Request.Path.ToString()
                .Replace("\r", string.Empty)
                .Replace("\n", string.Empty);

            _logger.LogWarning("403 Error - Access denied: {Path}", safePath);
        }
    }
}
