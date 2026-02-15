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
            _logger.LogWarning("403 Error - Access denied: {Path}", HttpContext.Request.Path);
        }
    }
}
