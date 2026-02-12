using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WebApplication1.Model;
using WebApplication1.Services;

namespace WebApplication1.Pages
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEncryptionService _encryptionService;
        private readonly IAuditService _auditService;

        public IndexModel(
            ILogger<IndexModel> logger,
            UserManager<ApplicationUser> userManager,
            IEncryptionService encryptionService,
            IAuditService auditService)
        {
            _logger = logger;
            _userManager = userManager;
            _encryptionService = encryptionService;
            _auditService = auditService;
        }

        public ApplicationUser? CurrentUser { get; set; }
        public string? DecryptedCreditCard { get; set; }

        public async Task OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                CurrentUser = user;
                DecryptedCreditCard = _encryptionService.Decrypt(user.CreditCard);

                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                var userAgent = HttpContext.Request.Headers["User-Agent"].ToString();

                await _auditService.LogActivityAsync(
                    user.Id,
                    "View Profile",
                    $"User viewed their profile from {ipAddress}",
                    ipAddress,
                    userAgent);
            }
        }
    }
}
