using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WebApplication1.Model;
using WebApplication1.ViewModels;
using WebApplication1.Services;

namespace WebApplication1.Pages
{
    public class RegisterModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IEncryptionService _encryptionService;
        private readonly IAuditService _auditService;
        private readonly IReCaptchaService _reCaptchaService;
        private readonly IInputSanitizationService _sanitizationService;
        private readonly IPasswordManagementService _passwordManagement;
        private readonly ILogger<RegisterModel> _logger;

        public RegisterModel(
            UserManager<ApplicationUser> userManager, 
            SignInManager<ApplicationUser> signInManager,
            IWebHostEnvironment webHostEnvironment,
            IEncryptionService encryptionService,
            IAuditService auditService,
            IReCaptchaService reCaptchaService,
            IInputSanitizationService sanitizationService,
            IPasswordManagementService passwordManagement,
            ILogger<RegisterModel> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _webHostEnvironment = webHostEnvironment;
            _encryptionService = encryptionService;
            _auditService = auditService;
            _reCaptchaService = reCaptchaService;
            _sanitizationService = sanitizationService;
            _passwordManagement = passwordManagement;
            _logger = logger;
        }

        [BindProperty]
        public required Register RModel { get; set; }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (ModelState.IsValid)
            {
                // Verify reCAPTCHA token
                var recaptchaToken = Request.Form["g-recaptcha-response"].ToString();
                var isRecaptchaValid = await _reCaptchaService.VerifyTokenAsync(recaptchaToken, "register");

                if (!isRecaptchaValid)
                {
                    ModelState.AddModelError(string.Empty, "reCAPTCHA validation failed. Please try again.");
                    return Page();
                }

                // Additional security checks for XSS and SQL injection attempts
                // Note: Address fields are allowed special characters and are HTML-encoded for safety
                var fieldsToCheck = new Dictionary<string, string>
                {
                    { "FirstName", RModel.FirstName },
                    { "LastName", RModel.LastName },
                    { "Email", RModel.Email },
                    { "Mobile", RModel.Mobile }
                    // Billing and Shipping are checked by AddressValidationAttribute and HTML-encoded
                };

                foreach (var field in fieldsToCheck)
                {
                    if (_sanitizationService.ContainsPotentialXss(field.Value))
                    {
                        _logger.LogWarning("Potential XSS attack detected in field {Field}: {Value}", field.Key, field.Value);
                        ModelState.AddModelError($"RModel.{field.Key}", "Invalid characters detected. Please remove any special characters or HTML.");
                        return Page();
                    }

                    if (_sanitizationService.ContainsPotentialSqlInjection(field.Value))
                    {
                        _logger.LogWarning("Potential SQL injection detected in field {Field}: {Value}", field.Key, field.Value);
                        ModelState.AddModelError($"RModel.{field.Key}", "Invalid characters detected. Please remove any SQL keywords or special characters.");
                        return Page();
                    }
                }

                // Sanitize inputs before processing
                var sanitizedFirstName = _sanitizationService.SanitizeInput(RModel.FirstName);
                var sanitizedLastName = _sanitizationService.SanitizeInput(RModel.LastName);
                var sanitizedMobile = _sanitizationService.SanitizeInput(RModel.Mobile);
                var sanitizedBilling = _sanitizationService.SanitizeInput(RModel.Billing);
                var sanitizedShipping = _sanitizationService.SanitizeInput(RModel.Shipping);
                var sanitizedEmail = _sanitizationService.SanitizeInput(RModel.Email);
                var sanitizedCreditCard = _sanitizationService.SanitizeInput(RModel.CreditCard);

                // Remove spaces from mobile and credit card for database storage (security best practice)
                // Spaces are for display formatting only - we store the raw digits
                sanitizedMobile = sanitizedMobile.Replace(" ", "");
                sanitizedCreditCard = sanitizedCreditCard.Replace(" ", "");

                // Aggressively HTML-encode address fields to encode ALL special characters
                var encodedBilling = _sanitizationService.AggressiveHtmlEncode(sanitizedBilling);
                var encodedShipping = _sanitizationService.AggressiveHtmlEncode(sanitizedShipping);

                // Validate photo is actually a JPG/JPEG
                if (RModel.Photo != null)
                {
                    var extension = Path.GetExtension(RModel.Photo.FileName).ToLowerInvariant();
                    if (extension != ".jpg" && extension != ".jpeg")
                    {
                        ModelState.AddModelError("RModel.Photo", "Only .jpg and .jpeg files are allowed.");
                        return Page();
                    }

                    // Additional file validation - check file size (max 5MB)
                    if (RModel.Photo.Length > 5 * 1024 * 1024)
                    {
                        ModelState.AddModelError("RModel.Photo", "Photo size cannot exceed 5MB.");
                        return Page();
                    }

                    // Validate file content type
                    var allowedContentTypes = new[] { "image/jpeg", "image/jpg" };
                    if (!allowedContentTypes.Contains(RModel.Photo.ContentType.ToLowerInvariant()))
                    {
                        ModelState.AddModelError("RModel.Photo", "Invalid file type. Only JPEG images are allowed.");
                        return Page();
                    }
                }

                // Save the photo file
                string photoPath = string.Empty;
                if (RModel.Photo != null)
                {
                    var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "photos");
                    Directory.CreateDirectory(uploadsFolder);

                    var uniqueFileName = Guid.NewGuid().ToString() + ".jpg";
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await RModel.Photo.CopyToAsync(fileStream);
                    }

                    photoPath = "/uploads/photos/" + uniqueFileName;
                }

                if (string.IsNullOrEmpty(photoPath))
                {
                    ModelState.AddModelError("RModel.Photo", "Photo upload failed.");
                    return Page();
                }

                // Create user with sanitized and encrypted data
                var user = new ApplicationUser()
                {
                    UserName = sanitizedEmail,
                    Email = sanitizedEmail,
                    FirstName = sanitizedFirstName,
                    LastName = sanitizedLastName,
                    CreditCard = _encryptionService.Encrypt(sanitizedCreditCard),
                    Mobile = sanitizedMobile,
                    Billing = encodedBilling,  // HTML-encoded for XSS prevention
                    Shipping = encodedShipping,  // HTML-encoded for XSS prevention
                    PhotoPath = photoPath,
                    LastPasswordChangeDate = DateTime.UtcNow,
                    PasswordExpiryDate = DateTime.UtcNow.AddDays(90),
                    MustChangePassword = false
                };

                var result = await _userManager.CreateAsync(user, RModel.Password);

                if (result.Succeeded)
                {
                    // Add password to history
                    var passwordHash = _userManager.PasswordHasher.HashPassword(user, RModel.Password);
                    await _passwordManagement.AddPasswordToHistoryAsync(user.Id, passwordHash);

                    var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                    var userAgent = HttpContext.Request.Headers["User-Agent"].ToString();

                    await _auditService.LogActivityAsync(
                        user.Id,
                        "Registration",
                        $"New user registered from {ipAddress}",
                        ipAddress,
                        userAgent);

                    _logger.LogInformation("New user registered successfully: {Email}", sanitizedEmail);

                    await _signInManager.SignInAsync(user, false);
                    return RedirectToPage("Index");
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
            }
            return Page();
        }
    }
}
