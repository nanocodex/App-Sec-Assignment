# reCAPTCHA Login Troubleshooting Checklist

## Immediate Steps to Diagnose the Issue

### 1. Test the reCAPTCHA Setup
Navigate to: **https://localhost:{port}/ReCaptchaTest**

This page will show you:
- ? Your configuration values
- ? Whether the reCAPTCHA script loads
- ? Whether tokens are generated
- ? Whether validation succeeds
- ? Real-time debug console

### 2. Check Browser Console (F12)
When you try to login, check for:
- JavaScript errors (red text)
- Console log messages showing:
  - "DOM loaded, setting up reCAPTCHA"
  - "Form submit triggered"
  - "Token received, length: XXX"
  - "Submitting form..."

### 3. Check Application Logs
Look for these log entries:
```
Attempting login for {email}. reCAPTCHA token present: True, Token length: XXX
reCAPTCHA API Response: {...}
reCAPTCHA validation successful/failed. Success: True/False, Score: X.X, Action: login
```

## Common Issues and Solutions

### Issue 1: Token Not Generated (length: 0)
**Symptoms:** Browser console shows "Token received, length: 0" or token missing

**Solutions:**
1. Check if reCAPTCHA script loads in Network tab (F12 ? Network)
2. Verify Site Key in appsettings.json is correct
3. Check if ad blocker is blocking Google domains
4. Clear browser cache and try again

### Issue 2: Score Too Low (Score < 0.5)
**Symptoms:** Logs show "Score: 0.1" or "Score: 0.3"

**Solutions:**
1. Lower threshold in appsettings.json:
   ```json
   "ScoreThreshold": 0.3
   ```
2. Avoid rapid-fire testing (looks like bot behavior)
3. Interact with page naturally (click around, wait a few seconds)
4. Try from a different browser/device

### Issue 3: Action Mismatch
**Symptoms:** Logs show "Action: '', Expected Action: login"

**Solutions:**
1. Token might be stale - refresh page and try again
2. Verify JavaScript action parameter matches: `{action: 'login'}`
3. Check if multiple reCAPTCHA scripts are loading

### Issue 4: Invalid Site Key
**Symptoms:** Console error: "Invalid site key"

**Solutions:**
1. Verify Site Key in appsettings.json
2. Check Google reCAPTCHA Admin Console
3. Ensure you're using v3 keys (not v2)
4. Verify domain is registered (add "localhost")

### Issue 5: Network/API Error
**Symptoms:** Error calling Google's API

**Solutions:**
1. Check internet connectivity
2. Verify firewall isn't blocking Google domains
3. Check if behind corporate proxy
4. Temporarily disable antivirus/firewall

## Step-by-Step Debugging Process

### Step 1: Verify Configuration
```bash
# Check your appsettings.json has:
"ReCaptcha": {
  "SiteKey": "6Lf...",      # Should start with 6L
  "SecretKey": "6Lf...",    # Should start with 6L
  "ScoreThreshold": 0.5     # Between 0.0 and 1.0
}
```

### Step 2: Test in Browser
1. Open browser (Chrome/Edge)
2. Press F12 to open Developer Tools
3. Go to Console tab
4. Navigate to /Login
5. Enter credentials
6. Click Login
7. Watch console for messages

### Step 3: Check Server Logs
1. In Visual Studio, open Output window (View ? Output)
2. Select "Debug" or "ASP.NET Core Web Server" from dropdown
3. Look for reCAPTCHA related messages
4. Note the Score and Success values

### Step 4: Use Test Page
1. Navigate to /ReCaptchaTest
2. Click "Submit Test"
3. Watch the Debug Console on the page
4. Check the result message
5. Check Visual Studio Output logs

## Expected Success Flow

### Client Side (Browser Console):
```
[timestamp] DOM loaded, setting up reCAPTCHA
[timestamp] Form submit triggered. formSubmitted: false
[timestamp] grecaptcha is loaded, executing...
[timestamp] grecaptcha ready, executing with action: login
[timestamp] Token received, length: 500+
[timestamp] Submitting form...
```

### Server Side (Application Logs):
```
info: Attempting login for user@example.com. reCAPTCHA token present: True, Token length: 500+
debug: reCAPTCHA API Response: {"success":true,"score":0.9,"action":"login",...}
info: reCAPTCHA validation successful. Score: 0.9, Action: login
info: User logged in successfully: user@example.com
```

## Quick Fixes to Try First

1. **Lower the threshold**:
   ```json
   "ScoreThreshold": 0.3
   ```

2. **Clear browser cache**: Ctrl+Shift+Delete

3. **Try incognito/private window**: Rules out extensions

4. **Wait 5-10 seconds** on login page before submitting

5. **Click around the page** before logging in (looks more human)

6. **Check you're using correct domain**: 
   - Google Admin Console should have "localhost" added
   - Site Key should match your domain registration

## If Still Failing

1. Share the browser console logs
2. Share the application logs (especially reCAPTCHA service logs)
3. Visit /ReCaptchaTest and share the result
4. Check if you can access https://www.google.com/recaptcha/
5. Try generating new keys in Google Admin Console

## Need New Keys?

If you suspect key issues:
1. Go to https://www.google.com/recaptcha/admin
2. Create new site (v3)
3. Add "localhost" and "127.0.0.1" as domains
4. Copy new Site Key and Secret Key
5. Update appsettings.json
6. Restart application
