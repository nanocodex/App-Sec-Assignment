using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WebApplication1.Services;

namespace WebApplication1.Pages
{
    public class ReCaptchaTestModel : PageModel
    {
        private readonly IReCaptchaService _reCaptchaService;
        private readonly ILogger<ReCaptchaTestModel> _logger;

        public ReCaptchaTestModel(IReCaptchaService reCaptchaService, ILogger<ReCaptchaTestModel> logger)
        {
            _reCaptchaService = reCaptchaService;
            _logger = logger;
        }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var recaptchaToken = Request.Form["g-recaptcha-response"].ToString();
            
            _logger.LogInformation("=== reCAPTCHA Test Form Submitted ===");
            _logger.LogInformation("Token present: {TokenPresent}", !string.IsNullOrEmpty(recaptchaToken));
            _logger.LogInformation("Token length: {TokenLength}", recaptchaToken?.Length ?? 0);
            
            if (!string.IsNullOrEmpty(recaptchaToken))
            {
                _logger.LogInformation("Token preview: {TokenPreview}...", recaptchaToken.Substring(0, Math.Min(50, recaptchaToken.Length)));
            }

            var isValid = await _reCaptchaService.VerifyTokenAsync(recaptchaToken, "test");
            
            _logger.LogInformation("Validation result: {IsValid}", isValid);
            _logger.LogInformation("=== End reCAPTCHA Test ===");

            TempData["ValidationResult"] = isValid ? "SUCCESS" : "FAILED";
            TempData["Message"] = isValid 
                ? "reCAPTCHA validation passed! Check the application logs for details." 
                : "reCAPTCHA validation failed. Check the application logs for error details.";

            return RedirectToPage();
        }
    }
}
