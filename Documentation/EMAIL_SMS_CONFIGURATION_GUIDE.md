# Email and SMS Configuration Guide

## Overview
Your Razor Pages web application already has Email and SMS services implemented for password reset functionality. This guide will help you configure them to work with real providers.

## Current Implementation

### Email Service
- **Interface**: `Services/IEmailService.cs`
- **Implementation**: `Services/EmailService.cs`
- **Features**:
  - Password reset emails with clickable links
  - Password changed notifications
  - HTML email templates
  - SMTP support with SSL/TLS

### SMS Service
- **Interface**: `Services/ISmsService.cs`
- **Implementation**: `Services/SmsService.cs`
- **Features**:
  - Password reset codes via SMS
  - 10-minute expiration for codes
  - Ready for SMS provider integration (Twilio, etc.)

## Configuration Options

### Option 1: Demo Mode (Current - For Testing)

**Status**: ? Already configured  
**Best for**: Development and testing

In this mode, emails and SMS are logged to the console instead of being sent. This is what you're currently using.

**Configuration** (`appsettings.json`):
```json
{
  "Email": {
    "SmtpHost": "",
    "SmtpPort": 587,
    "SmtpUsername": "",
    "SmtpPassword": "",
    "FromEmail": "noreply@appsec.com"
  },
  "SMS": {
    "Provider": ""
  }
}
```

**Testing**:
1. Trigger a password reset
2. Check the **Debug Console** or **Output window** in Visual Studio
3. You'll see the email content or SMS message logged

**Example log output**:
```
EMAIL (SMTP not configured):
To: user@example.com
Subject: Password Reset Request
Body:
<html>
  <body>
    <h2>Password Reset Request</h2>
    <p>You have requested to reset your password. Click the link below:</p>
    <p><a href='https://localhost:7xxx/ResetPassword?token=...'>Reset Password</a></p>
    ...
  </body>
</html>
```

---

## Option 2: Gmail SMTP (Free for Testing)

**Best for**: Personal projects, testing with real emails

### Step 1: Enable App Password in Gmail

1. Go to your Google Account: https://myaccount.google.com/
2. Navigate to **Security**
3. Enable **2-Step Verification** (if not already enabled)
4. Go to **App passwords**: https://myaccount.google.com/apppasswords
5. Select **Mail** and **Other (Custom name)**
6. Name it: "Razor Pages App"
7. Click **Generate**
8. **Copy the 16-character password** (you won't see it again!)

### Step 2: Update appsettings.json

```json
{
  "Email": {
    "SmtpHost": "smtp.gmail.com",
    "SmtpPort": 587,
    "SmtpUsername": "your-email@gmail.com",
    "SmtpPassword": "your-16-char-app-password",
    "FromEmail": "your-email@gmail.com"
  }
}
```

### Step 3: Test

1. Run your application
2. Go to `/ForgotPassword`
3. Enter a valid email address
4. Check your inbox - you should receive the password reset email!

**Important Notes**:
- ?? **Never commit** `appsettings.json` with real credentials to Git
- ? Use **User Secrets** for development (see below)
- ? Use **Environment Variables** for production

---

## Option 3: Microsoft 365 / Outlook SMTP

**Best for**: Business/school accounts

### Configuration

```json
{
  "Email": {
    "SmtpHost": "smtp-mail.outlook.com",
    "SmtpPort": 587,
    "SmtpUsername": "your-email@outlook.com",
    "SmtpPassword": "your-password",
    "FromEmail": "your-email@outlook.com"
  }
}
```

**Note**: Modern authentication may require app-specific passwords.

---

## Option 4: SendGrid (Free Tier Available)

**Best for**: Production applications  
**Free tier**: 100 emails/day forever

### Step 1: Create SendGrid Account

1. Sign up at: https://signup.sendgrid.com/
2. Verify your email
3. Create an **API Key**:
   - Go to **Settings** ? **API Keys**
   - Click **Create API Key**
   - Name it: "RazorPagesApp"
   - Set permissions: **Full Access** or **Mail Send**
   - Copy the API key (you won't see it again!)

### Step 2: Update EmailService.cs

You'll need to modify the `EmailService.cs` to use SendGrid's API instead of SMTP:

```csharp
// Install NuGet package: SendGrid
// PM> Install-Package SendGrid

using SendGrid;
using SendGrid.Helpers.Mail;

private async Task<bool> SendEmailAsync(string toEmail, string subject, string body)
{
    try
    {
        var apiKey = _configuration["Email:SendGrid:ApiKey"];
        
        if (string.IsNullOrEmpty(apiKey))
        {
            // Fallback to logging
            _logger.LogInformation("EMAIL (SendGrid not configured): ...");
            return true;
        }

        var client = new SendGridClient(apiKey);
        var from = new EmailAddress(_configuration["Email:FromEmail"], "AppSec Team");
        var to = new EmailAddress(toEmail);
        var msg = MailHelper.CreateSingleEmail(from, to, subject, 
            "Please enable HTML to view this email.", body);
        
        var response = await client.SendEmailAsync(msg);
        
        if (response.IsSuccessStatusCode)
        {
            _logger.LogInformation("Email sent successfully to {Email}", toEmail);
            return true;
        }
        
        _logger.LogError("SendGrid returned status code: {StatusCode}", response.StatusCode);
        return false;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to send email to {Email}", toEmail);
        return false;
    }
}
```

### Step 3: Update appsettings.json

```json
{
  "Email": {
    "SendGrid": {
      "ApiKey": "SG.xxxxxxxxxxxxxxxxxxxxx"
    },
    "FromEmail": "noreply@yourdomain.com"
  }
}
```

---

## SMS Configuration Options

### Option 1: Demo Mode (Current)

**Status**: ? Already configured  
**Configuration**: Keep `"Provider": ""` in `appsettings.json`

**Testing**: Check console logs for SMS messages

---

### Option 2: Twilio (Most Popular)

**Best for**: Production applications  
**Free trial**: $15.50 credit

### Step 1: Create Twilio Account

1. Sign up at: https://www.twilio.com/try-twilio
2. Verify your phone number
3. Get your **Account SID** and **Auth Token** from the dashboard
4. Get a Twilio phone number

### Step 2: Install Twilio NuGet Package

```powershell
Install-Package Twilio
```

Or using .NET CLI:
```bash
dotnet add package Twilio
```

### Step 3: Update SmsService.cs

```csharp
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

public async Task<bool> SendPasswordResetSmsAsync(string mobile, string resetCode)
{
    try
    {
        var message = $"Your password reset code is: {resetCode}. This code will expire in 10 minutes. Do not share this code with anyone.";
        
        var accountSid = _configuration["SMS:Twilio:AccountSid"];
        var authToken = _configuration["SMS:Twilio:AuthToken"];
        var fromNumber = _configuration["SMS:Twilio:FromNumber"];
        
        if (string.IsNullOrEmpty(accountSid))
        {
            // Fallback to logging
            _logger.LogInformation("SMS (Provider not configured): To: {Mobile}, Message: {Message}", 
                mobile, message);
            return true;
        }
        
        TwilioClient.Init(accountSid, authToken);
        
        var messageResource = await MessageResource.CreateAsync(
            body: message,
            from: new PhoneNumber(fromNumber),
            to: new PhoneNumber($"+65{mobile}") // Singapore country code
        );
        
        _logger.LogInformation("SMS sent successfully to {Mobile}. SID: {Sid}", 
            mobile, messageResource.Sid);
        return true;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to send SMS to {Mobile}", mobile);
        return false;
    }
}
```

### Step 4: Update appsettings.json

```json
{
  "SMS": {
    "Provider": "Twilio",
    "Twilio": {
      "AccountSid": "ACxxxxxxxxxxxxxxxxxxxxx",
      "AuthToken": "your-auth-token",
      "FromNumber": "+1234567890"
    }
  }
}
```

---

## Securing Configuration Secrets

### For Development: Use User Secrets

**Never commit sensitive data to Git!** Use ASP.NET Core's User Secrets feature:

1. **Right-click your project** in Visual Studio
2. Select **Manage User Secrets**
3. Add your configuration:

```json
{
  "Email": {
    "SmtpHost": "smtp.gmail.com",
    "SmtpPort": 587,
    "SmtpUsername": "your-email@gmail.com",
    "SmtpPassword": "your-app-password",
    "FromEmail": "your-email@gmail.com"
  },
  "SMS": {
    "Provider": "Twilio",
    "Twilio": {
      "AccountSid": "ACxxxxxxxxxxxxxxxxxxxxx",
      "AuthToken": "your-auth-token",
      "FromNumber": "+1234567890"
    }
  }
}
```

4. **Keep `appsettings.json` with empty values** for Git

### For Production: Use Environment Variables

On your production server, set environment variables:

**Windows**:
```powershell
$env:Email__SmtpHost = "smtp.gmail.com"
$env:Email__SmtpUsername = "your-email@gmail.com"
$env:Email__SmtpPassword = "your-password"
$env:SMS__Twilio__AccountSid = "ACxxxxx"
$env:SMS__Twilio__AuthToken = "your-token"
```

**Linux/Azure**:
```bash
export Email__SmtpHost="smtp.gmail.com"
export Email__SmtpUsername="your-email@gmail.com"
export Email__SmtpPassword="your-password"
```

**Azure App Service**:
1. Go to **Configuration** ? **Application settings**
2. Add each setting as a new entry

---

## Testing Your Configuration

### Test Email

1. Run your application
2. Navigate to `/ForgotPassword`
3. Enter your email address
4. Click **Send Reset Link**
5. Check your email inbox

**Troubleshooting**:
- Check the **Output window** in Visual Studio for logs
- Verify SMTP credentials are correct
- Check your email provider's security settings
- For Gmail, ensure "Less secure app access" is OFF and you're using an **App Password**

### Test SMS

1. Run your application
2. Navigate to `/ForgotPassword` (if it supports SMS option)
3. Or go to `/ResetPasswordSms`
4. Enter a valid Singapore mobile number (e.g., `81234567`)
5. Click **Send Code**
6. Check your phone for the SMS

**Troubleshooting**:
- Check console logs for errors
- Verify Twilio credentials
- Ensure phone number format is correct (`+65` for Singapore)
- Check Twilio account balance (trial accounts have credit limits)

---

## Current Features Using Email/SMS

Your application uses these services for:

1. **Password Reset via Email** (`/ForgotPassword`)
   - Sends a secure link to reset password
   - Link expires in 1 hour
   - Includes user-friendly HTML template

2. **Password Reset via SMS** (`/ResetPasswordSms`)
   - Sends a 6-digit code
   - Code expires in 10 minutes
   - Singapore mobile number format validation

3. **Password Changed Notifications**
   - Automatic email when password is changed
   - Security alert feature

---

## Recommended Setup for Your Project

### For Assignment/Testing:
? **Use Demo Mode** (current setup)
- No configuration needed
- Check logs to verify emails/SMS would be sent
- Mention in documentation that services are in demo mode

### For Production Deployment:
1. **Email**: Use SendGrid (free tier)
2. **SMS**: Use Twilio (trial credit)
3. **Secrets**: Use Azure Key Vault or environment variables
4. **Monitoring**: Log all send attempts for auditing

---

## Security Best Practices

1. ? **Never commit credentials** to Git
   - Use User Secrets for development
   - Use environment variables for production

2. ? **Use App-Specific Passwords**
   - Gmail and Outlook require app passwords when 2FA is enabled

3. ? **Rate Limiting**
   - Your app already has lockout protection (3 failed attempts)
   - Consider adding email/SMS rate limits to prevent abuse

4. ? **Audit Logging**
   - Your app already logs all email/SMS send attempts
   - Check `AuditLogs` table for security monitoring

5. ? **Validate Recipients**
   - Your app validates email format and mobile number format
   - Never send to unverified addresses in production

---

## Cost Comparison

| Provider | Service | Free Tier | Paid Plan |
|----------|---------|-----------|-----------|
| Gmail SMTP | Email | Limited (user's quota) | N/A (personal use) |
| SendGrid | Email | 100/day forever | $14.95/month (40k emails) |
| Twilio | SMS | $15.50 trial credit | Pay-as-you-go (~$0.0079/SMS) |
| AWS SES | Email | 62,000/month (if on EC2) | $0.10 per 1,000 emails |
| Vonage (Nexmo) | SMS | Trial credit | ~$0.008/SMS |

---

## Quick Start Guide

### For Testing (5 minutes):
1. Keep current configuration (demo mode)
2. Test `/ForgotPassword` and `/ResetPasswordSms`
3. Check **Debug Console** for logged messages
4. ? Done! No configuration needed.

### For Real Emails (15 minutes):
1. Create Gmail App Password (see Option 2 above)
2. Right-click project ? **Manage User Secrets**
3. Add Gmail SMTP configuration
4. Test `/ForgotPassword`
5. ? Done! Emails will be sent to real addresses.

### For Real SMS (30 minutes):
1. Sign up for Twilio account
2. Get Account SID, Auth Token, and phone number
3. Install Twilio NuGet package: `Install-Package Twilio`
4. Update `SmsService.cs` with Twilio code (see Option 2 above)
5. Right-click project ? **Manage User Secrets**
6. Add Twilio configuration
7. Test `/ResetPasswordSms`
8. ? Done! SMS will be sent to real phones.

---

## Support and Documentation

- **ASP.NET Core Configuration**: https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/
- **User Secrets**: https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets
- **SendGrid Docs**: https://docs.sendgrid.com/
- **Twilio Docs**: https://www.twilio.com/docs/

---

## Summary

Your application **already has Email and SMS services implemented**. They're currently in **demo mode** (logging instead of sending), which is perfect for development and testing.

**To activate real sending**:
- **Email**: Add SMTP credentials to User Secrets (Gmail is easiest for testing)
- **SMS**: Add Twilio credentials and update `SmsService.cs` code

**For your assignment**: The current demo mode is sufficient - just mention in your documentation that the services are implemented and would work with real providers when configured.

---

## Need Help?

If you encounter issues:
1. Check the **Debug Console** or **Output window** for error logs
2. Verify your credentials are correct
3. Check your email/SMS provider's security settings
4. Review the configuration section that matches your provider

Your services are production-ready - they just need provider credentials to start sending real emails and SMS!
