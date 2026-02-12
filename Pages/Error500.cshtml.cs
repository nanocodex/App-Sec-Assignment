using Microsoft.AspNetCore.Mvc.RazorPages;

namespace WebApplication1.Pages
{
    public class Error500Model : PageModel
    {
        private readonly ILogger<Error500Model> _logger;

        public Error500Model(ILogger<Error500Model> logger)
        {
            _logger = logger;
        }

        public void OnGet()
        {
            _logger.LogError("500 Error - Internal server error occurred");
        }
    }
}
