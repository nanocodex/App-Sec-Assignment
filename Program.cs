using Microsoft.AspNetCore.Identity;
using WebApplication1.Model;
using WebApplication1.Services;
using WebApplication1.Middleware;
using System.Text.Encodings.Web;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

// Configure session
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(1);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
});

builder.Services.AddDbContext<AuthDbContext>();

// Configure Identity with custom password validator and options
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // Configure password requirements (these work alongside our custom validator)
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 12;
    options.Password.RequiredUniqueChars = 1;

    // Configure lockout settings
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(1);
    options.Lockout.MaxFailedAccessAttempts = 3;
    options.Lockout.AllowedForNewUsers = true;

    // Configure user settings
    options.User.RequireUniqueEmail = true;

    // Configure sign-in settings
    options.SignIn.RequireConfirmedEmail = false;
    
    // Configure token settings for 2FA
    options.Tokens.AuthenticatorTokenProvider = TokenOptions.DefaultAuthenticatorProvider;
})
.AddEntityFrameworkStores<AuthDbContext>()
.AddDefaultTokenProviders()
.AddPasswordValidator<CustomPasswordValidator>();

// Configure application cookie
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Login";
    options.LogoutPath = "/Logout";
    options.AccessDeniedPath = "/Error403";
    options.ExpireTimeSpan = TimeSpan.FromMinutes(1);
    options.SlidingExpiration = true;
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
});

// Add data protection
builder.Services.AddDataProtection();

// Register HtmlEncoder for input sanitization
builder.Services.AddSingleton(HtmlEncoder.Default);

// Register encryption service
builder.Services.AddScoped<IEncryptionService, EncryptionService>();

// Register audit service
builder.Services.AddScoped<IAuditService, AuditService>();

// Register session service
builder.Services.AddScoped<ISessionService, SessionService>();

// Register input sanitization service
builder.Services.AddScoped<IInputSanitizationService, InputSanitizationService>();

// Register reCAPTCHA service
builder.Services.AddHttpClient<IReCaptchaService, ReCaptchaService>();

// Register Two-Factor Authentication service
builder.Services.AddScoped<ITwoFactorService, TwoFactorService>();

// Register background service for session cleanup
builder.Services.AddHostedService<SessionCleanupService>();

// Configure antiforgery token options for CSRF protection
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
else
{
    app.UseExceptionHandler("/Error");
}

// Configure custom error pages for specific status codes
app.UseStatusCodePagesWithReExecute("/Error{0}");

// Add security headers
app.Use(async (context, next) =>
{
    // Content Security Policy - helps prevent XSS attacks
    context.Response.Headers.Append("Content-Security-Policy", 
        "default-src 'self'; " +
        "script-src 'self' 'unsafe-inline' 'unsafe-eval' https://www.google.com/recaptcha/ https://www.gstatic.com/recaptcha/ https://cdn.jsdelivr.net; " +
        "style-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net; " +
        "img-src 'self' data: https:; " +
        "font-src 'self' https://cdn.jsdelivr.net; " +
        "frame-src https://www.google.com/recaptcha/; " +
        "connect-src 'self' https://www.google.com/recaptcha/");
    
    // X-Content-Type-Options - prevents MIME sniffing
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    
    // X-Frame-Options - prevents clickjacking
    context.Response.Headers.Append("X-Frame-Options", "SAMEORIGIN");
    
    // X-XSS-Protection - enables XSS filtering
    context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
    
    // Referrer-Policy - controls referrer information
    context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
    
    // Permissions-Policy - controls browser features
    context.Response.Headers.Append("Permissions-Policy", "geolocation=(), microphone=(), camera=()");
    
    await next();
});

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession(); // Enable session middleware

app.UseAuthentication();

app.UseAuthorization();

// Add session validation middleware
app.UseSessionValidation();

app.MapRazorPages();

app.Run();
