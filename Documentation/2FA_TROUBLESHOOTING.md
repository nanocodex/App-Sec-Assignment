# 2FA Troubleshooting Guide

## Common Issues and Solutions

### Issue 1: "Invalid verification code" Error

#### Symptom
User enters the 6-digit code from their authenticator app, but gets "Invalid verification code" error.

#### Possible Causes & Solutions

**1. Time Synchronization Issue** (Most Common)
- **Problem**: Server and device clocks are not synchronized
- **Solution for Users**:
  ```
  iOS:
  Settings ? General ? Date & Time ? Enable "Set Automatically"
  
  Android:
  Settings ? System ? Date & Time ? Enable "Use network-provided time"
  
  Windows:
  Settings ? Time & Language ? Date & Time ? Enable "Set time automatically"
  ```

- **Solution for Admins**:
  ```powershell
  # Check server time
  Get-Date
  
  # Sync with time server
  w32tm /resync
  ```

**2. Code Expired**
- **Problem**: TOTP codes refresh every 30 seconds
- **Solution**: Wait for the code to refresh in the app, then enter the new code quickly

**3. Wrong Account Selected**
- **Problem**: User has multiple accounts in authenticator app
- **Solution**: Verify the account name in the authenticator app matches the current login

**4. Time Zone Mismatch**
- **Problem**: Server and client in different time zones with incorrect UTC calculation
- **Solution**: Verify both server and client are using UTC correctly

---

### Issue 2: QR Code Won't Scan

#### Symptom
Authenticator app can't read the QR code on the Enable 2FA page.

#### Solutions

**1. Use Manual Entry**
```
1. On the Enable 2FA page, look below the QR code
2. You'll see a text code like: ABCD EFGH IJKL MNOP
3. In your authenticator app, tap "Enter key manually"
4. Type the code exactly as shown (spaces don't matter)
5. Select "Time-based" when prompted
6. Enter account name (e.g., your email)
```

**2. Improve QR Code Visibility**
- Increase screen brightness to maximum
- Ensure screen is clean (no smudges)
- Hold phone steady, 6-8 inches from screen
- Try different lighting conditions

**3. Try Different App**
- Google Authenticator
- Microsoft Authenticator
- Authy

**4. Use Desktop App** (If using mobile browser)
- Authy has desktop versions for Windows/Mac/Linux
- Can manually enter code on desktop

---

### Issue 3: Lost Authenticator Device

#### Symptom
User's phone is lost, stolen, or broken, and they can't access authenticator app.

#### Solutions

**Solution 1: Use Recovery Code** (Recommended)
```
1. On the 2FA verification page during login
2. Click "Use a recovery code instead"
3. Enter one of the saved recovery codes
4. Successfully login
5. IMMEDIATELY go to Manage 2FA page
6. Generate new recovery codes
7. Save the new codes securely
```

**Solution 2: Restore Authenticator App** (If backed up)
- **Google Authenticator**: 
  - Requires manual re-setup (no cloud backup in older versions)
  - Newer versions support Google Account sync
  
- **Microsoft Authenticator**: 
  - Restore from cloud backup
  - Sign in with Microsoft account
  
- **Authy**: 
  - Multi-device sync enabled by default
  - Install on new device and login

**Solution 3: Admin Override** (Last Resort)
```sql
-- Admin must manually disable 2FA in database
-- See "Admin Guide" section in 2FA_IMPLEMENTATION_GUIDE.md
```

---

### Issue 4: Lost Recovery Codes

#### Symptom
User lost both authenticator device AND recovery codes.

#### Solutions

**If User Still Has Access (Can Login):**
```
1. Login with 2FA
2. Navigate to /Manage2FA
3. Click "Generate New Recovery Codes"
4. Save the new codes immediately
```

**If User Lost BOTH Device and Codes:**
```
NO SELF-SERVICE SOLUTION AVAILABLE
This is intentional for security reasons.

Required Steps:
1. Contact system administrator
2. Verify identity (may require multiple forms of ID)
3. Admin manually disables 2FA in database
4. User can login without 2FA
5. User should re-enable 2FA with new device
```

**Prevention:**
- Save recovery codes in password manager (1Password, LastPass, Bitwarden)
- Print recovery codes and store in safe place
- Take encrypted backup of recovery codes

---

### Issue 5: Database Migration Errors

#### Symptom
Error when running `dotnet ef database update` for Add2FASupport migration.

#### Error Messages & Solutions

**Error: "The column 'IsTwoFactorEnabled' conflicts with an existing column"**
```
Solution 1: Check if column already exists
SELECT COLUMN_NAME 
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'AspNetUsers' 
AND COLUMN_NAME = 'IsTwoFactorEnabled'

If exists, remove the migration:
dotnet ef migrations remove
Then re-create with different column name or modify migration manually
```

**Error: "Cannot insert NULL into column 'IsTwoFactorEnabled'"**
```
Solution: Migration should set default value

In migration file, ensure:
migrationBuilder.AddColumn<bool>(
    name: "IsTwoFactorEnabled",
    table: "AspNetUsers",
    nullable: false,
    defaultValue: false);
```

**Error: "Foreign key constraint failed"**
```
This shouldn't happen with 2FA migration
If it does, check for orphaned records:

SELECT * FROM AspNetUserTokens 
WHERE UserId NOT IN (SELECT Id FROM AspNetUsers)

Delete orphaned records:
DELETE FROM AspNetUserTokens
WHERE UserId NOT IN (SELECT Id FROM AspNetUsers)
```

---

### Issue 6: "2FA is already enabled" Error

#### Symptom
User tries to enable 2FA but gets error message.

#### Solutions

**Solution 1: Check Current Status**
```
1. Go to /Manage2FA page
2. Check if 2FA status shows "ENABLED"
3. If yes, 2FA is already active
```

**Solution 2: Reset 2FA**
```
1. Go to /Manage2FA
2. Click "Disable Two-Factor Authentication"
3. Confirm the action
4. You'll be logged out
5. Login again (without 2FA)
6. Go to /Enable2FA
7. Set up 2FA fresh
```

**Solution 3: Database Inconsistency**
```sql
-- Check 2FA status in database
SELECT 
    Email,
    TwoFactorEnabled,       -- Identity default
    IsTwoFactorEnabled      -- Custom property
FROM AspNetUsers
WHERE Email = 'user@example.com'

-- If inconsistent, fix manually:
UPDATE AspNetUsers
SET TwoFactorEnabled = IsTwoFactorEnabled
WHERE Email = 'user@example.com'
```

---

### Issue 7: Recovery Code Not Working

#### Symptom
User enters recovery code but gets "Invalid recovery code" error.

#### Possible Causes & Solutions

**1. Already Used**
- **Problem**: Recovery codes are one-time use only
- **Solution**: Try a different recovery code

**2. Formatting Issue**
```
Recovery codes can be entered with or without dashes:
? ABCD-1234
? ABCD1234
? abcd-1234  (case-insensitive)
? abcd1234

System automatically handles all formats
```

**3. Wrong Recovery Codes**
- **Problem**: User generated new codes and is using old ones
- **Solution**: Verify codes are the most recent set

**4. Encryption Key Changed**
```
If Data Protection keys were reset, recovery codes can't be decrypted.

Check in database:
SELECT TwoFactorRecoveryCodes FROM AspNetUsers WHERE Email = 'user@example.com'

If encrypted but can't decrypt, admin must:
1. Disable 2FA for user
2. User re-enables 2FA
3. User gets new recovery codes
```

---

### Issue 8: "Remember This Device" Not Working

#### Symptom
User checks "Remember this device" but is still prompted for 2FA on next login.

#### Possible Causes & Solutions

**1. Cookies Cleared**
- **Problem**: Browser cookies were cleared
- **Solution**: Normal behavior, re-check "Remember this device"

**2. Incognito/Private Mode**
- **Problem**: Private browsing doesn't persist cookies
- **Solution**: Use normal browser window

**3. Cookie Security Settings**
```csharp
// In Program.cs, verify:
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;  // Requires HTTPS
    options.Cookie.SameSite = SameSiteMode.Strict;
});

Ensure:
- Application is running on HTTPS
- Cookie settings allow authentication cookies
```

**4. Different Browser/Device**
- **Problem**: "Remember" is per browser/device
- **Solution**: Each browser/device needs to be remembered separately

---

### Issue 9: Build Errors After Adding 2FA

#### Error: "CS0246: The type or namespace name 'QRCoder' could not be found"

**Solution:**
```bash
dotnet restore
dotnet build

Or manually add package:
dotnet add package QRCoder --version 1.6.0
```

#### Error: "SetAuthenticationTokenAsync not found"

**Solution:**
Ensure using Microsoft.AspNetCore.Identity namespace:
```csharp
using Microsoft.AspNetCore.Identity;
```

#### Error: "GetAuthenticationTokenAsync not found"

**Solution:**
Add token providers in Program.cs:
```csharp
builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<AuthDbContext>()
    .AddDefaultTokenProviders()  // ? Required for token storage
    .AddPasswordValidator<CustomPasswordValidator>();
```

---

### Issue 10: Audit Logs Not Recording 2FA Events

#### Symptom
2FA works but no entries in AuditLogs table.

#### Solutions

**1. Verify Audit Service Registered**
```csharp
// In Program.cs, check:
builder.Services.AddScoped<IAuditService, AuditService>();
```

**2. Check Database Connection**
```sql
-- Verify AuditLogs table exists
SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'AuditLogs'

-- Check for recent entries
SELECT TOP 10 * FROM AuditLogs ORDER BY Timestamp DESC
```

**3. Check for Exceptions**
```
Look in application logs:
- Visual Studio Output window
- IIS logs
- Application Insights (if configured)

Search for:
- "AuditService"
- "LogActivityAsync"
- Exception stack traces
```

**4. Test Directly**
```csharp
// In Enable2FA.cshtml.cs OnPostAsync, add logging:
_logger.LogInformation("About to log audit entry for user {UserId}", currentUser.Id);

await _auditService.LogActivityAsync(
    currentUser.Id,
    "2FA Enabled",
    $"Two-factor authentication enabled from {ipAddress}",
    ipAddress,
    userAgent);
    
_logger.LogInformation("Audit entry logged successfully");
```

---

## Developer Debugging Tips

### Enable Detailed Logging

In `appsettings.Development.json`:
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "WebApplication1.Services.TwoFactorService": "Debug",
      "WebApplication1.Pages.Verify2FAModel": "Debug"
    }
  }
}
```

### Test TOTP Manually

```csharp
// In Verify2FA.cshtml.cs, add before verification:
_logger.LogDebug("Secret Key: {Key}", authenticatorKey);
_logger.LogDebug("User Code: {Code}", code);
_logger.LogDebug("Current Time: {Time}", DateTimeOffset.UtcNow.ToUnixTimeSeconds());

// Generate expected code for debugging
var expectedCode = _twoFactorService.GenerateTotpCode(authenticatorKey, 
    DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 30);
_logger.LogDebug("Expected Code: {Code}", expectedCode);
```

### Verify Time Synchronization

```csharp
// In TwoFactorService.cs, VerifyTotpCode method:
var unixTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
_logger.LogDebug("Current Unix Timestamp: {Timestamp}", unixTimestamp);
_logger.LogDebug("Time Window: {Window}", unixTimestamp / 30);

for (int i = -1; i <= 1; i++)
{
    var timeWindow = (unixTimestamp / 30) + i;
    var expectedCode = GenerateTotpCode(secretKey, timeWindow);
    _logger.LogDebug("Window {Window}: Expected Code {Code}", i, expectedCode);
}
```

### Check Database State

```sql
-- Full 2FA status for a user
SELECT 
    u.Id,
    u.Email,
    u.TwoFactorEnabled AS 'Identity_2FA',
    u.IsTwoFactorEnabled AS 'Custom_2FA',
    CASE 
        WHEN t.Value IS NULL THEN 'No Key'
        ELSE 'Key Exists (Length: ' + CAST(LEN(t.Value) AS VARCHAR) + ')'
    END AS 'AuthenticatorKey',
    CASE
        WHEN u.TwoFactorRecoveryCodes IS NULL THEN 'No Codes'
        ELSE 'Codes Exist (Length: ' + CAST(LEN(u.TwoFactorRecoveryCodes) AS VARCHAR) + ')'
    END AS 'RecoveryCodes'
FROM AspNetUsers u
LEFT JOIN AspNetUserTokens t 
    ON u.Id = t.UserId 
    AND t.LoginProvider = '[AspNetUserStore]'
    AND t.Name = 'AuthenticatorKey'
WHERE u.Email = 'test@example.com'

-- Recent 2FA audit logs
SELECT 
    a.Timestamp,
    a.Action,
    a.IpAddress,
    a.Details
FROM AuditLogs a
INNER JOIN AspNetUsers u ON a.UserId = u.Id
WHERE u.Email = 'test@example.com'
AND a.Action LIKE '%2FA%'
ORDER BY a.Timestamp DESC
```

---

## Emergency Procedures

### Emergency: Disable 2FA for All Users

**?? WARNING: Only use in critical situations**

```sql
BEGIN TRANSACTION

-- Disable 2FA for all users
UPDATE AspNetUsers
SET TwoFactorEnabled = 0,
    IsTwoFactorEnabled = 0,
    TwoFactorRecoveryCodes = NULL

-- Remove all authenticator keys
DELETE FROM AspNetUserTokens
WHERE LoginProvider = '[AspNetUserStore]'
AND Name = 'AuthenticatorKey'

-- Log the action
INSERT INTO AuditLogs (UserId, Action, Timestamp, IpAddress, Details)
SELECT 
    Id as UserId,
    '2FA Disabled - Emergency Override' as Action,
    GETUTCDATE() as Timestamp,
    'System' as IpAddress,
    'Emergency 2FA disable for all users' as Details
FROM AspNetUsers

-- REVIEW CHANGES BEFORE COMMITTING
-- ROLLBACK  -- Undo changes
COMMIT  -- Apply changes
```

### Emergency: Reset Single User's 2FA

```sql
DECLARE @UserEmail NVARCHAR(256) = 'user@example.com'
DECLARE @UserId NVARCHAR(450)

SELECT @UserId = Id FROM AspNetUsers WHERE Email = @UserEmail

IF @UserId IS NOT NULL
BEGIN
    -- Disable 2FA
    UPDATE AspNetUsers
    SET TwoFactorEnabled = 0,
        IsTwoFactorEnabled = 0,
        TwoFactorRecoveryCodes = NULL
    WHERE Id = @UserId
    
    -- Remove authenticator key
    DELETE FROM AspNetUserTokens
    WHERE UserId = @UserId
    AND LoginProvider = '[AspNetUserStore]'
    AND Name = 'AuthenticatorKey'
    
    -- Log the action
    INSERT INTO AuditLogs (UserId, Action, Timestamp, IpAddress, Details)
    VALUES (
        @UserId,
        '2FA Disabled - Admin Override',
        GETUTCDATE(),
        'Admin',
        'Manual 2FA reset due to lost device and recovery codes'
    )
    
    PRINT 'Successfully disabled 2FA for ' + @UserEmail
END
ELSE
BEGIN
    PRINT 'User not found: ' + @UserEmail
END
```

---

## Testing Checklist

Use this checklist to verify 2FA is working correctly:

### Basic Functionality
- [ ] Enable 2FA page loads without errors
- [ ] QR code displays correctly
- [ ] Manual entry key is shown
- [ ] Valid code is accepted
- [ ] Invalid code is rejected
- [ ] Recovery codes are generated (10 codes)
- [ ] Recovery codes can be downloaded
- [ ] Recovery codes can be printed

### Login Flow
- [ ] Login without 2FA works for non-2FA users
- [ ] Login with password redirects to Verify2FA
- [ ] Correct 6-digit code allows login
- [ ] Incorrect code shows error
- [ ] "Remember device" checkbox works
- [ ] Recovery code login works
- [ ] Used recovery code is removed
- [ ] Low recovery code warning shows

### Management
- [ ] Manage 2FA shows correct status
- [ ] Can generate new recovery codes
- [ ] Can disable 2FA
- [ ] Disabling 2FA logs out user
- [ ] Re-enabling 2FA generates new secret

### Database
- [ ] IsTwoFactorEnabled set correctly
- [ ] TwoFactorRecoveryCodes encrypted
- [ ] Authenticator key stored in AspNetUserTokens
- [ ] Audit logs created for all actions

### Security
- [ ] QR code not accessible after setup
- [ ] Recovery codes shown only once
- [ ] Account lockout works with 2FA
- [ ] Time drift tolerance works (±30 sec)
- [ ] Case-insensitive recovery code entry

---

## Support Resources

### Documentation
- **Full Guide**: `2FA_IMPLEMENTATION_GUIDE.md`
- **Quick Start**: `2FA_QUICK_START.md`
- **This Guide**: `2FA_TROUBLESHOOTING.md`

### Code References
- **Service**: `Services/TwoFactorService.cs`
- **Enable Page**: `Pages/Enable2FA.cshtml.cs`
- **Verify Page**: `Pages/Verify2FA.cshtml.cs`
- **Manage Page**: `Pages/Manage2FA.cshtml.cs`

### External Resources
- **RFC 6238 (TOTP)**: https://tools.ietf.org/html/rfc6238
- **RFC 4226 (HOTP)**: https://tools.ietf.org/html/rfc4226
- **Google Authenticator**: https://github.com/google/google-authenticator
- **OWASP 2FA**: https://cheatsheetseries.owasp.org/cheatsheets/Multifactor_Authentication_Cheat_Sheet.html

---

**Last Updated**: 2024
**Version**: 1.0
**Status**: Complete ?
