# Google reCAPTCHA v3 Implementation Guide

## Overview
Google reCAPTCHA v3 has been successfully integrated into your Razor Pages application. It protects both the Login and Register pages from bot attacks.

## Configuration Steps

### 1. Get Your reCAPTCHA Keys
1. Visit [Google reCAPTCHA Admin Console](https://www.google.com/recaptcha/admin)
2. Click "Create" or "+" to register a new site
3. Fill in the form:
   - **Label**: Your application name (e.g., "IT2163 Assignment")
   - **reCAPTCHA type**: Select "reCAPTCHA v3"
   - **Domains**: Add your domains (e.g., "localhost" for development, your production domain)
4. Accept the terms and click "Submit"
5. Copy both the **Site Key** and **Secret Key**

### 2. Update appsettings.json
Replace the placeholder values in `appsettings.json`:

```json
"ReCaptcha": {
  "SiteKey": "YOUR_ACTUAL_SITE_KEY_HERE",
  "SecretKey": "YOUR_ACTUAL_SECRET_KEY_HERE",
  "ScoreThreshold": 0.5
}
```

**Note**: 
- `ScoreThreshold` ranges from 0.0 to 1.0
- 0.5 is recommended (blocks suspicious traffic)
- Lower values (e.g., 0.3) = more lenient
- Higher values (e.g., 0.7) = more strict

### 3. Security Best Practices
For production environments, consider storing the Secret Key in:
- **Azure Key Vault**
- **Environment Variables**
- **User Secrets** (for development)

Never commit your actual Secret Key to source control!

## How It Works

### Login Page (`/Login`)
- When the user submits the login form, reCAPTCHA v3 generates a token
- The token is verified server-side against Google's API
- Action: "login"
- If verification fails, the login attempt is blocked

### Register Page (`/Register`)
- When the user submits the registration form, reCAPTCHA v3 generates a token
- The token is verified server-side against Google's API
- Action: "register"
- If verification fails, the registration attempt is blocked

## Features Implemented

? **IReCaptchaService Interface** - Service contract for reCAPTCHA verification
? **ReCaptchaService** - Implementation with HTTP client for Google API calls
? **Configuration in appsettings.json** - Site Key, Secret Key, and Score Threshold
? **Service Registration** - Registered in Program.cs with HttpClient
? **Login Protection** - reCAPTCHA verification on login attempts
? **Register Protection** - reCAPTCHA verification on registration
? **Client-side Integration** - JavaScript to generate tokens invisibly
? **Logging** - Failed verification attempts are logged for security monitoring

## Testing

1. **Valid Submissions**: Normal user behavior should pass without issues
2. **Bot Detection**: Automated scripts will receive low scores and be blocked
3. **Monitoring**: Check application logs for reCAPTCHA validation failures

## Score Interpretation

reCAPTCHA v3 returns a score (0.0 - 1.0):
- **1.0**: Very likely a legitimate user
- **0.5**: Neutral (default threshold)
- **0.0**: Very likely a bot

The score threshold (0.5) determines what's acceptable. Adjust based on your needs.

## Troubleshooting

### Common Issues:

1. **"reCAPTCHA validation failed" when using autofill**
   - **FIXED**: The scripts now use `DOMContentLoaded` and proper form state management
   - The form waits for reCAPTCHA to fully load before submitting
   - A loading indicator appears during verification

2. **Empty reCAPTCHA token**
   - Check browser console (F12) for JavaScript errors
   - Ensure the reCAPTCHA script is loading from Google
   - Verify your Site Key is correct in appsettings.json
   - Make sure you're not blocking Google domains in browser/firewall

3. **"reCAPTCHA validation failed"**
   - Check that Site Key and Secret Key are correct
   - Verify domain is registered in reCAPTCHA admin console
   - For localhost testing, ensure "localhost" is added to domains
   - Check application logs for detailed error messages (Score, Action mismatch)
   - Check network connectivity to Google's API

4. **Script not loading**
   - Ensure you have internet connectivity
   - Check browser console for errors
   - Verify Site Key is correctly injected in _Layout.cshtml
   - Try clearing browser cache

5. **Low scores for legitimate users**
   - Lower the ScoreThreshold in appsettings.json (try 0.3 or 0.4)
   - Review user behavior patterns in reCAPTCHA admin console
   - Check if you're testing too quickly (automation-like behavior)

6. **Localhost domain issues**
   - In Google reCAPTCHA Admin Console, add both:
     - `localhost` (for development)
     - `127.0.0.1` (alternative localhost address)
   - Clear browser cache after adding domains
   - Wait a few minutes for changes to propagate

### Debugging Tips:

1. **Enable detailed logging** in appsettings.json:
   ```json
   "Logging": {
     "LogLevel": {
       "Default": "Information",
       "WebApplication1.Services.ReCaptchaService": "Debug"
     }
   }
   ```

2. **Check browser console** (F12) for JavaScript errors

3. **Check application logs** for reCAPTCHA validation details including:
   - Success status
   - Score received
   - Action name
   - Error codes

4. **Test with manual typing** (not autofill) to verify reCAPTCHA works

5. **Verify API connectivity** by checking network tab in browser developer tools
