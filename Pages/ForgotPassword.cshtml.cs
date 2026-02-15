using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WebApplication1.Model;
using WebApplication1.Services;
using WebApplication1.ViewModels;

namespace WebApplication1.Pages
{
    public class ForgotPasswordModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IPasswordManagementService _passwordManagement;
        private readonly IEmailService _emailService;
        private readonly ISmsService _smsService;
        private readonly IAuditService _auditService;
        private readonly ILogger<ForgotPasswordModel> _logger;

        public ForgotPasswordModel(
            UserManager<ApplicationUser> userManager,
            IPasswordManagementService passwordManagement,
            IEmailService emailService,
            ISmsService smsService,
            IAuditService auditService,
            ILogger<ForgotPasswordModel> logger)
        {
            _userManager = userManager;
            _passwordManagement = passwordManagement;
            _emailService = emailService;
            _smsService = smsService;
            _auditService = auditService;
            _logger = logger;
        }

        [BindProperty]
        public required ForgotPassword Model { get; set; }

        public string? SuccessMessage { get; set; }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var user = await _userManager.FindByEmailAsync(Model.Email);
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var userAgent = HttpContext.Request.Headers["User-Agent"].ToString();

            // Always show success message to prevent email enumeration
            SuccessMessage = Model.UseSms 
                ? "If an account with this email exists, an SMS with reset code has been sent to your registered mobile number."
                : "If an account with this email exists, a password reset link has been sent to your email address.";

            if (user == null)
            {
                _logger.LogWarning("Password reset requested for non-existent email: {Email}", Model.Email);
                // Don't reveal that the user doesn't exist
                return Page();
            }

            // Generate reset token
            var (success, token) = await _passwordManagement.GeneratePasswordResetTokenAsync(user.Id);
            if (!success)
            {
                _logger.LogError("Failed to generate password reset token for user {UserId}", user.Id);
                return Page();
            }

            if (Model.UseSms)
            {
                // Send SMS with reset code
                var resetCode = _passwordManagement.GenerateSmsResetCode();
                
                // Store the code in session temporarily (expires in 10 minutes)
                HttpContext.Session.SetString($"PasswordResetCode_{user.Id}", resetCode);
                HttpContext.Session.SetString($"PasswordResetCodeExpiry_{user.Id}", DateTime.UtcNow.AddMinutes(10).ToString("o"));
                
                await _smsService.SendPasswordResetSmsAsync(user.Mobile, resetCode);
                
                await _auditService.LogActivityAsync(
                    user.Id,
                    "Password Reset Requested - SMS",
                    $"Password reset code sent via SMS from {ipAddress}",
                    ipAddress,
                    userAgent);

                // Redirect to SMS reset page
                return RedirectToPage("/ResetPasswordSms", new { email = Model.Email });
            }
            else
            {
                // Send email with reset link
                var resetLink = Url.Page(
                    "/ResetPassword",
                    pageHandler: null,
                    values: new { userId = user.Id, token = token },
                    protocol: Request.Scheme);

                await _emailService.SendPasswordResetEmailAsync(user.Email!, resetLink!);

                await _auditService.LogActivityAsync(
                    user.Id,
                    "Password Reset Requested - Email",
                    $"Password reset link sent via email from {ipAddress}",
                    ipAddress,
                    userAgent);
            }

            _logger.LogInformation("Password reset initiated for user {UserId}", user.Id);
            return Page();
        }
    }
}
