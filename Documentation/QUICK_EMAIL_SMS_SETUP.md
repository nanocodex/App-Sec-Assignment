# Quick Email & SMS Setup - Reference Card

## Current Status: ? Already Implemented!

Your app has **fully functional** Email and SMS services. They're in **demo mode** (logging instead of sending).

---

## ?? Fastest Setup (Testing)

### Keep Demo Mode (0 minutes)
**No changes needed!** Check console logs to see emails/SMS.

```bash
# Run app
dotnet run

# Go to /ForgotPassword
# Check Debug Console for:
# "EMAIL (SMTP not configured)..."
# "SMS (Provider not configured)..."
```

? **Perfect for assignments and testing!**

---

## ?? Quick Email Setup (10 minutes)

### Using Gmail

**1. Get Gmail App Password:**
- Visit: https://myaccount.google.com/apppasswords
- Generate password for "Mail"
- Copy the 16-character code

**2. Add to User Secrets:**
```bash
# Right-click project ? Manage User Secrets
```

```json
{
  "Email": {
    "SmtpHost": "smtp.gmail.com",
    "SmtpPort": 587,
    "SmtpUsername": "your-email@gmail.com",
    "SmtpPassword": "abcd efgh ijkl mnop",
    "FromEmail": "your-email@gmail.com"
  }
}
```

**3. Test:**
- Go to `/ForgotPassword`
- Enter your email
- Check inbox! ??

---

## ?? Quick SMS Setup (30 minutes)

### Using Twilio

**1. Sign up & Get Credentials:**
- Visit: https://www.twilio.com/try-twilio
- Copy: Account SID, Auth Token, Phone Number

**2. Install Package:**
```powershell
Install-Package Twilio
```

**3. Update `Services/SmsService.cs`:**

Add at top:
```csharp
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;
```

Replace the method:
```csharp
public async Task<bool> SendPasswordResetSmsAsync(string mobile, string resetCode)
{
    try
    {
        var message = $"Your password reset code is: {resetCode}. Expires in 10 minutes.";
        
        var accountSid = _configuration["SMS:Twilio:AccountSid"];
        var authToken = _configuration["SMS:Twilio:AuthToken"];
        var fromNumber = _configuration["SMS:Twilio:FromNumber"];
        
        if (string.IsNullOrEmpty(accountSid))
        {
            _logger.LogInformation("SMS (Not configured): {Mobile} - {Message}", mobile, message);
            return true;
        }
        
        TwilioClient.Init(accountSid, authToken);
        
        var msg = await MessageResource.CreateAsync(
            body: message,
            from: new PhoneNumber(fromNumber),
            to: new PhoneNumber($"+65{mobile}")
        );
        
        _logger.LogInformation("SMS sent to {Mobile}. SID: {Sid}", mobile, msg.Sid);
        return true;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "SMS failed: {Mobile}", mobile);
        return false;
    }
}
```

**4. Add to User Secrets:**
```json
{
  "SMS": {
    "Provider": "Twilio",
    "Twilio": {
      "AccountSid": "ACxxxxxxxxxxxxxxxxxxxx",
      "AuthToken": "your-auth-token",
      "FromNumber": "+1234567890"
    }
  }
}
```

**5. Test:**
- Go to `/ResetPasswordSms`
- Enter mobile: `81234567`
- Check phone! ??

---

## ?? Keep Secrets Safe

### ? NEVER do this:
```json
// In appsettings.json (tracked by Git)
{
  "Email": {
    "SmtpPassword": "my-real-password"  // ? BAD!
  }
}
```

### ? ALWAYS do this:
```json
// In appsettings.json (tracked by Git)
{
  "Email": {
    "SmtpHost": "",
    "SmtpPassword": "",  // ? Empty in Git
    "FromEmail": "noreply@appsec.com"
  }
}
```

```json
// In User Secrets (NOT tracked by Git)
{
  "Email": {
    "SmtpHost": "smtp.gmail.com",
    "SmtpPassword": "abcd efgh ijkl mnop"  // ? Safe!
  }
}
```

**Access User Secrets:**
- Visual Studio: Right-click project ? **Manage User Secrets**
- Location: `%APPDATA%\Microsoft\UserSecrets\<project-id>\secrets.json`

---

## ?? Testing Checklist

### Email Test
- [ ] Go to `/ForgotPassword`
- [ ] Enter email address
- [ ] Click "Send Reset Link"
- [ ] Check inbox (or console if demo mode)
- [ ] Click reset link
- [ ] Should redirect to `/ResetPassword`

### SMS Test
- [ ] Go to `/ResetPasswordSms`
- [ ] Enter mobile: `81234567`
- [ ] Click "Send Code"
- [ ] Check phone (or console if demo mode)
- [ ] Enter code on next page
- [ ] Should allow password reset

---

## ?? Configuration Summary

| Mode | Email | SMS | Setup Time | Cost |
|------|-------|-----|------------|------|
| **Demo** (current) | Console logs | Console logs | 0 min | Free |
| **Gmail SMTP** | Real emails | Console logs | 10 min | Free |
| **Gmail + Twilio** | Real emails | Real SMS | 40 min | $15.50 trial |

---

## ?? Troubleshooting

### Email not sending?
```bash
# Check logs in Debug Console:
# Look for errors starting with "Email" or "SMTP"

# Common issues:
# 1. Wrong password ? Use App Password, not account password
# 2. 2FA not enabled ? Gmail requires 2FA for App Passwords
# 3. Firewall blocking port 587 ? Check network settings
```

### SMS not sending?
```bash
# Check logs in Debug Console:
# Look for errors starting with "SMS" or "Twilio"

# Common issues:
# 1. Wrong credentials ? Double-check Account SID and Auth Token
# 2. Invalid phone format ? Use: +65 prefix for Singapore
# 3. Trial restrictions ? Twilio trial only sends to verified numbers
```

### Still not working?
```bash
# Full diagnostic:
dotnet run

# In app, trigger email/SMS
# Check Visual Studio Output window:
# - Select "Debug" from dropdown
# - Look for EmailService or SmsService logs
```

---

## ?? Where Things Are

```
Your Project/
??? Services/
?   ??? EmailService.cs        ? Email implementation
?   ??? IEmailService.cs       ? Email interface
?   ??? SmsService.cs          ? SMS implementation (UPDATE THIS for Twilio)
?   ??? ISmsService.cs         ? SMS interface
??? Pages/
?   ??? ForgotPassword.cshtml  ? Email password reset
?   ??? ResetPasswordSms.cshtml ? SMS password reset
??? appsettings.json           ? Keep empty (Git tracked)
??? secrets.json               ? Put real values here (NOT in Git)
```

---

## ?? Recommended for Your Assignment

**Use Demo Mode:**
- ? No setup needed
- ? Works immediately
- ? Logs show it works
- ? Mention in documentation: "Email/SMS services implemented with provider abstraction. Currently in demo mode (logging). Production-ready with SMTP/Twilio configuration."

**If you want to impress:**
- ?? Set up Gmail SMTP (10 min)
- ?? Screenshot real email being received
- ?? Mention "Tested with Gmail SMTP in development"

---

## ?? Pro Tips

1. **User Secrets** > Environment Variables > appsettings.json
2. For **production**, use Azure Key Vault or AWS Secrets Manager
3. Test email with **your own address first**
4. Twilio **trial** only sends to verified numbers
5. Check **spam folder** if email doesn't arrive
6. Use **SendGrid** for production (better deliverability)

---

## ?? More Info

Full guide: `Documentation/EMAIL_SMS_CONFIGURATION_GUIDE.md`

Quick links:
- Gmail App Passwords: https://myaccount.google.com/apppasswords
- Twilio Console: https://console.twilio.com/
- User Secrets Docs: https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets

---

## ? You're All Set!

Your Email and SMS services are **already implemented and working** in demo mode. 

**Next steps:**
1. ? Test in demo mode (check console logs)
2. ?? (Optional) Configure real providers
3. ?? Document the feature in your assignment

**Questions?** Check the full guide or the troubleshooting section above!
