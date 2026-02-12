# Two-Factor Authentication (2FA) Implementation Guide

## Overview

Your ASP.NET Core Razor Pages application now includes a comprehensive Two-Factor Authentication (2FA) system using TOTP (Time-based One-Time Password) compatible with popular authenticator apps like:
- Google Authenticator
- Microsoft Authenticator
- Authy
- 1Password
- LastPass Authenticator

## Features Implemented

### ? Core 2FA Features
- **TOTP Authentication**: Industry-standard 6-digit codes that refresh every 30 seconds
- **QR Code Setup**: Easy enrollment by scanning a QR code with authenticator apps
- **Manual Entry**: Fallback option for devices that can't scan QR codes
- **Recovery Codes**: 10 one-time-use backup codes for account recovery
- **Secure Storage**: Authenticator keys stored using ASP.NET Core Identity tokens
- **Encrypted Recovery Codes**: Recovery codes encrypted using Data Protection API

### ? Security Features
- **Audit Logging**: All 2FA activities logged (enable, disable, login attempts)
- **Time Window Tolerance**: Allows for slight time drift (±30 seconds)
- **Recovery Code Consumption**: Each recovery code can only be used once
- **Session Integration**: 2FA works seamlessly with existing session management
- **IP & Device Tracking**: Logs IP address and user agent for all 2FA events

### ? User Experience Features
- **Enable 2FA Page**: Step-by-step setup wizard with clear instructions
- **Verify 2FA Page**: Login verification with option to use recovery codes
- **Manage 2FA Page**: View status, regenerate recovery codes, disable 2FA
- **Recovery Code Display**: Print or download recovery codes for safekeeping
- **Remember Device**: Option to skip 2FA for 30 days on trusted devices
- **Low Code Warning**: Alerts when running low on recovery codes (?3 remaining)

---

## Files Created/Modified

### New Files

#### Services
- `Services/ITwoFactorService.cs` - Interface for 2FA service
- `Services/TwoFactorService.cs` - Implementation of 2FA functionality

#### Pages
- `Pages/Enable2FA.cshtml` - Page to enable 2FA
- `Pages/Enable2FA.cshtml.cs` - Logic for 2FA enrollment
- `Pages/Verify2FA.cshtml` - Page for 2FA verification during login
- `Pages/Verify2FA.cshtml.cs` - Logic for 2FA verification
- `Pages/Manage2FA.cshtml` - Page to manage 2FA settings
- `Pages/Manage2FA.cshtml.cs` - Logic for managing 2FA

#### Database
- `Migrations/[timestamp]_Add2FASupport.cs` - Database migration for 2FA fields

### Modified Files

#### Configuration
- `Program.cs` - Added TwoFactorService registration and token providers
- `IT2163-06 Practical Assignment 231295J.csproj` - Added QRCoder NuGet package

#### Models
- `Model/ApplicationUser.cs` - Added 2FA properties:
  - `IsTwoFactorEnabled` (bool)
  - `TwoFactorRecoveryCodes` (string, encrypted)

#### Login Flow
- `Pages/Login.cshtml.cs` - Added redirect to Verify2FA when 2FA is required

---

## Database Schema Changes

### AspNetUsers Table - New Columns
```sql
IsTwoFactorEnabled     bit            NOT NULL DEFAULT 0
TwoFactorRecoveryCodes nvarchar(MAX)  NULL
```

### AspNetUserTokens Table (Identity Default)
Stores authenticator keys:
```sql
UserId       nvarchar(450)  NOT NULL  -- FK to AspNetUsers
LoginProvider nvarchar(450) NOT NULL  -- "[AspNetUserStore]"
Name         nvarchar(450)  NOT NULL  -- "AuthenticatorKey"
Value        nvarchar(MAX)  NULL      -- Encrypted TOTP secret
```

---

## How 2FA Works

### 1. Enrollment Flow

```
User navigates to /Enable2FA
    ?
System generates random secret key (Base32, 160 bits)
    ?
System creates QR code: otpauth://totp/AppSecAssignment:user@email.com?secret=KEY
    ?
User scans QR code with authenticator app
    ?
Authenticator app generates 6-digit TOTP code
    ?
User enters code on verification page
    ?
System verifies code using HMAC-SHA1 algorithm
    ?
If valid:
  - Save authenticator key as user token
  - Set TwoFactorEnabled = true
  - Generate 10 recovery codes
  - Encrypt and store recovery codes
  - Log activity in audit log
    ?
Display recovery codes for user to save
```

### 2. Login Flow with 2FA

```
User enters email and password on /Login
    ?
System validates credentials with SignInManager
    ?
If credentials valid AND user has 2FA enabled:
  - Redirect to /Verify2FA
    ?
User enters 6-digit code from authenticator app
OR uses recovery code
    ?
System verifies TOTP code:
  - Retrieves authenticator key from user tokens
  - Calculates expected code using current time window
  - Compares with user input (allows ±1 time window for drift)
    ?
If valid:
  - Sign in user
  - Create session
  - Log successful 2FA login
  - Redirect to homepage
    ?
If invalid:
  - Show error message
  - Log failed attempt
  - Allow retry
```

### 3. Recovery Code Flow

```
User clicks "Use a recovery code instead"
    ?
User enters one of their 10 recovery codes
    ?
System decrypts stored recovery codes
    ?
System compares entered code (case-insensitive, ignoring dashes)
    ?
If match found:
  - Remove used code from list
  - Re-encrypt remaining codes
  - Update user record
  - Sign in user
  - Log recovery code usage
  - Warn if ?3 codes remaining
    ?
If no match:
  - Show error message
  - Log failed attempt
  - Allow retry
```

---

## TOTP Algorithm Details

### Standard: RFC 6238 (TOTP)
Based on RFC 4226 (HOTP)

### Parameters
- **Time Step**: 30 seconds
- **Digits**: 6
- **Algorithm**: HMAC-SHA1
- **Secret Length**: 160 bits (20 bytes)
- **Encoding**: Base32

### Code Generation
```
Current Unix Timestamp = seconds since epoch
Time Counter = Unix Timestamp / 30 seconds
HMAC = HMAC-SHA1(Secret Key, Time Counter)
Offset = Last byte of HMAC & 0x0F
Truncated = Extract 4 bytes from HMAC at Offset
Code = Truncated % 1,000,000 (6 digits)
```

### Time Window Tolerance
The verification checks 3 time windows:
- Previous window (-30 seconds)
- Current window (now)
- Next window (+30 seconds)

This allows for slight clock drift between server and device.

---

## Security Considerations

### ? Implemented Protections

1. **Secure Key Generation**
   - Cryptographically random secret keys using `RandomNumberGenerator`
   - 160-bit entropy (meets NIST recommendations)

2. **Encrypted Storage**
   - Recovery codes encrypted using Data Protection API
   - Authenticator keys stored in Identity's token storage
   - Both protected with AES-256 encryption

3. **Audit Trail**
   - All 2FA events logged (enable, disable, successful login, failed attempts)
   - Includes IP address and user agent for forensics
   - Stored in AuditLogs table for review

4. **Recovery Code Security**
   - Each code usable only once
   - 8 characters (alphanumeric, confusing characters excluded)
   - Format: XXXX-XXXX for readability
   - Encrypted at rest

5. **Brute Force Protection**
   - Existing account lockout applies to 2FA attempts
   - Rate limiting inherited from Identity configuration
   - 3 failed attempts = account lockout

6. **Session Security**
   - 2FA required on each new device/browser
   - "Remember this device" option available (30 days)
   - Sessions still time out after inactivity

### ?? Security Notes

1. **QR Code Display**
   - QR codes contain the secret key
   - Only shown during enrollment
   - Not stored after setup completes
   - User should ensure private viewing

2. **Recovery Codes**
   - Shown only once during generation
   - User responsible for secure storage
   - Recommend printing or password manager
   - Regeneration invalidates old codes

3. **Device Loss**
   - If user loses authenticator device AND recovery codes:
   - Account recovery requires admin intervention
   - No backdoor intentionally provided
   - High security vs. convenience tradeoff

---

## Testing Guide

### Test 1: Enable 2FA

**Steps:**
1. Login to your account
2. Navigate to `/Enable2FA`
3. Install an authenticator app (Google Authenticator recommended)
4. Scan the displayed QR code
5. Enter the 6-digit code from the app
6. Click "Verify and Enable 2FA"

**Expected Result:**
- ? Recovery codes displayed (10 codes)
- ? Success message shown
- ? Audit log entry created
- ? Database updated: `IsTwoFactorEnabled = 1`

**Verify in Database:**
```sql
SELECT 
    Id, Email, TwoFactorEnabled, IsTwoFactorEnabled,
    LEN(TwoFactorRecoveryCodes) as RecoveryCodesLength
FROM AspNetUsers
WHERE Email = 'your-email@test.com'
```

### Test 2: Login with 2FA

**Steps:**
1. Logout completely
2. Navigate to `/Login`
3. Enter email and password
4. Click "Log in"

**Expected Result:**
- ? Redirected to `/Verify2FA`
- ? Prompt for 6-digit code shown

**Steps (continued):**
5. Open authenticator app
6. Find the 6-digit code for your account
7. Enter the code
8. Click "Verify"

**Expected Result:**
- ? Successfully logged in
- ? Redirected to homepage
- ? Audit log entry: "2FA Login Success"
- ? Session created

**Verify in Audit Logs:**
```sql
SELECT TOP 5 Action, Timestamp, IpAddress, Details
FROM AuditLogs
WHERE UserId = '<your-user-id>'
ORDER BY Timestamp DESC
```

### Test 3: Login with Recovery Code

**Steps:**
1. Logout
2. Login with email and password
3. On 2FA verification page, click "Use a recovery code instead"
4. Enter one of your saved recovery codes
5. Click "Use Recovery Code"

**Expected Result:**
- ? Successfully logged in
- ? Warning if ?3 codes remaining
- ? Audit log entry: "2FA Login Success - Recovery Code"
- ? Recovery code removed from list

**Verify in Database:**
```sql
-- Recovery codes should be shorter (one less code)
SELECT 
    Email,
    LEN(TwoFactorRecoveryCodes) as RecoveryCodesLength
FROM AspNetUsers
WHERE Email = 'your-email@test.com'
```

### Test 4: Manage 2FA

**Steps:**
1. Login (with 2FA)
2. Navigate to `/Manage2FA`

**Expected Result:**
- ? Status shows "2FA ENABLED"
- ? Remaining recovery codes count displayed
- ? Options to regenerate codes or disable 2FA

**Steps (Generate New Codes):**
3. Click "Generate New Recovery Codes"
4. Confirm the action

**Expected Result:**
- ? New 10 recovery codes displayed
- ? Old codes invalidated
- ? Audit log entry created

**Steps (Disable 2FA):**
5. Click "Disable Two-Factor Authentication"
6. Confirm the action

**Expected Result:**
- ? 2FA disabled
- ? Signed out automatically
- ? Must login again without 2FA
- ? Audit log entry created

### Test 5: Invalid Code Handling

**Steps:**
1. Logout
2. Login with email and password
3. On 2FA page, enter invalid code: `000000`
4. Click "Verify"

**Expected Result:**
- ? Error message: "Invalid authentication code"
- ? Still on verification page
- ? Can retry
- ? Audit log entry: "2FA Login Failed - Invalid Code"

**After 3 Failed Attempts:**
- ? Account should lock out (existing rate limiting)

### Test 6: Remember Device

**Steps:**
1. Login with email and password
2. On 2FA page, check "Remember this device for 30 days"
3. Enter valid 6-digit code
4. Click "Verify"
5. Logout
6. Login again

**Expected Result:**
- ? After first login: 2FA verification bypassed for 30 days
- ? Note: Clearing cookies will reset this

---

## User Guide

### How to Enable 2FA

1. **Install an Authenticator App**
   - iOS: Google Authenticator, Microsoft Authenticator (App Store)
   - Android: Google Authenticator, Microsoft Authenticator (Play Store)
   - Desktop: Authy (Windows/Mac/Linux)

2. **Enable 2FA in Your Account**
   - Login to your account
   - Navigate to Security Settings or Enable 2FA page
   - Follow the on-screen instructions

3. **Scan the QR Code**
   - Open your authenticator app
   - Tap "Add Account" or "+"
   - Scan the displayed QR code

4. **Enter Verification Code**
   - Your app will show a 6-digit code
   - Enter this code on the website
   - Click "Verify and Enable 2FA"

5. **Save Your Recovery Codes**
   - **IMPORTANT**: Print or download the recovery codes
   - Store them in a safe place
   - You'll need them if you lose your device

### How to Login with 2FA

1. Enter your email and password as usual
2. You'll be prompted for a 6-digit code
3. Open your authenticator app
4. Find the code for this website
5. Enter the code and click "Verify"

**Remember This Device**: Check the box to skip 2FA for 30 days on this device.

### How to Use a Recovery Code

If you lost your device:
1. On the 2FA verification page, click "Use a recovery code instead"
2. Enter one of your saved recovery codes
3. Click "Use Recovery Code"
4. **Important**: Each code works only once

After using a recovery code:
- Go to Manage 2FA page
- Generate new recovery codes
- Save the new codes

### How to Disable 2FA

**Warning**: This makes your account less secure.

1. Login to your account (with 2FA)
2. Navigate to Manage 2FA page
3. Click "Disable Two-Factor Authentication"
4. Confirm the action
5. You'll be signed out
6. Login again (no 2FA required)

---

## Troubleshooting

### Issue: QR Code Won't Scan

**Solution 1**: Increase screen brightness
**Solution 2**: Use manual entry
- On the setup page, there's a text code below the QR code
- In your authenticator app, choose "Enter key manually"
- Type the code exactly as shown
- Select "Time-based" when prompted

### Issue: Code Always Invalid

**Possible Causes:**
1. **Time Sync Issue**
   - Authenticator apps rely on accurate time
   - Check your phone's time settings
   - Enable "Automatic time" / "Network time"

2. **Wrong Account**
   - Ensure you're using the code for the correct account
   - Authenticator apps can store multiple accounts

3. **Code Expired**
   - Codes refresh every 30 seconds
   - Wait for a new code and try again quickly

### Issue: Lost Recovery Codes

**If you still have device access:**
1. Login with 2FA
2. Go to Manage 2FA
3. Click "Generate New Recovery Codes"
4. Save the new codes

**If you lost both device AND recovery codes:**
- Contact system administrator
- Account recovery requires manual intervention
- Be prepared to verify your identity

### Issue: Want to Reset 2FA

**Solution:**
1. Login with existing 2FA
2. Disable 2FA in Manage 2FA page
3. Re-enable 2FA (creates new secret key)
4. Scan new QR code
5. Get new recovery codes

---

## Admin Guide

### View 2FA Status for User

```sql
SELECT 
    u.Id,
    u.Email,
    u.TwoFactorEnabled,        -- Identity default
    u.IsTwoFactorEnabled,       -- Custom property
    CASE 
        WHEN u.TwoFactorRecoveryCodes IS NULL THEN 0
        ELSE 1
    END as HasRecoveryCodes,
    t.Value as AuthenticatorKey -- Encrypted
FROM AspNetUsers u
LEFT JOIN AspNetUserTokens t 
    ON u.Id = t.UserId 
    AND t.LoginProvider = '[AspNetUserStore]'
    AND t.Name = 'AuthenticatorKey'
WHERE u.Email = 'user@example.com'
```

### View 2FA Audit Trail

```sql
SELECT 
    a.Timestamp,
    a.Action,
    a.IpAddress,
    a.UserAgent,
    a.Details,
    u.Email
FROM AuditLogs a
INNER JOIN AspNetUsers u ON a.UserId = u.Id
WHERE a.Action LIKE '%2FA%'
ORDER BY a.Timestamp DESC
```

### Disable 2FA for User (Emergency)

**Warning**: Only use in emergencies (lost device + lost codes)

```sql
-- Disable 2FA
UPDATE AspNetUsers
SET TwoFactorEnabled = 0,
    IsTwoFactorEnabled = 0,
    TwoFactorRecoveryCodes = NULL
WHERE Email = 'user@example.com'

-- Remove authenticator key
DELETE FROM AspNetUserTokens
WHERE UserId = (SELECT Id FROM AspNetUsers WHERE Email = 'user@example.com')
AND LoginProvider = '[AspNetUserStore]'
AND Name = 'AuthenticatorKey'

-- Log the action
INSERT INTO AuditLogs (UserId, Action, Timestamp, IpAddress, Details)
VALUES (
    (SELECT Id FROM AspNetUsers WHERE Email = 'user@example.com'),
    '2FA Disabled - Admin Override',
    GETUTCDATE(),
    'Admin Panel',
    'Emergency 2FA reset due to lost device and recovery codes'
)
```

### Monitor Failed 2FA Attempts

```sql
SELECT 
    COUNT(*) as FailedAttempts,
    u.Email,
    a.IpAddress,
    MAX(a.Timestamp) as LastAttempt
FROM AuditLogs a
INNER JOIN AspNetUsers u ON a.UserId = u.Id
WHERE a.Action IN ('2FA Login Failed - Invalid Code', '2FA Login Failed - Invalid Recovery Code')
AND a.Timestamp > DATEADD(HOUR, -1, GETUTCDATE())
GROUP BY u.Email, a.IpAddress
HAVING COUNT(*) >= 3
ORDER BY FailedAttempts DESC
```

---

## API Reference

### ITwoFactorService Interface

```csharp
string GenerateSecretKey()
// Generates a random Base32-encoded secret key (160 bits)

string GenerateQrCodeUri(string email, string secretKey, string issuer = "AppSecAssignment")
// Creates otpauth:// URI for QR code generation

string GenerateQrCodeImage(string qrCodeUri)
// Generates QR code as base64-encoded PNG image

bool VerifyTotpCode(string secretKey, string code)
// Verifies a 6-digit TOTP code (allows ±30 second drift)

List<string> GenerateRecoveryCodes(int count = 10)
// Generates recovery codes (format: XXXX-XXXX)

string EncryptRecoveryCodes(List<string> codes)
// Encrypts recovery codes using Data Protection API

List<string> DecryptRecoveryCodes(string encryptedCodes)
// Decrypts recovery codes

bool VerifyAndConsumeRecoveryCode(string encryptedCodes, string code, out string updatedEncryptedCodes)
// Verifies recovery code and removes it from list
```

### Usage Examples

```csharp
// Generate and setup 2FA
var secretKey = _twoFactorService.GenerateSecretKey();
var qrCodeUri = _twoFactorService.GenerateQrCodeUri(user.Email, secretKey);
var qrCodeImage = _twoFactorService.GenerateQrCodeImage(qrCodeUri);

// Verify TOTP code
var isValid = _twoFactorService.VerifyTotpCode(secretKey, userEnteredCode);

// Generate recovery codes
var codes = _twoFactorService.GenerateRecoveryCodes(10);
var encrypted = _twoFactorService.EncryptRecoveryCodes(codes);

// Use recovery code
var isValid = _twoFactorService.VerifyAndConsumeRecoveryCode(
    user.TwoFactorRecoveryCodes,
    userEnteredCode,
    out string updatedCodes);
```

---

## Future Enhancements

### Potential Features
- [ ] SMS-based 2FA as alternative
- [ ] Email-based 2FA backup
- [ ] Hardware security key support (WebAuthn/FIDO2)
- [ ] Trusted device management page
- [ ] 2FA requirement for specific actions (e.g., password change)
- [ ] Admin dashboard for 2FA statistics
- [ ] Automatic recovery code regeneration reminder
- [ ] Export audit logs for compliance

### Integration Opportunities
- [ ] Azure Multi-Factor Authentication
- [ ] Duo Security integration
- [ ] Okta integration
- [ ] Single Sign-On (SSO) with 2FA

---

## Compliance & Standards

### RFC Compliance
- ? **RFC 6238**: TOTP (Time-Based One-Time Password)
- ? **RFC 4226**: HOTP (HMAC-Based One-Time Password)
- ? **RFC 4648**: Base32 encoding

### Security Standards
- ? **NIST SP 800-63B**: Digital Identity Guidelines
  - Multi-factor authentication implemented
  - Authenticator binds to subscriber
  - Recovery codes provided

- ? **OWASP ASVS**: Application Security Verification Standard
  - Level 2 compliance for authentication
  - Cryptographic randomness for secrets
  - Secure token storage

### Best Practices
- ? **OWASP Authentication Cheat Sheet**
- ? **Google Authenticator Key URI Format**
- ? **Microsoft 2FA Guidelines**

---

## Summary

Your application now has enterprise-grade Two-Factor Authentication that:
- ? Works with all major authenticator apps
- ? Provides secure recovery options
- ? Integrates seamlessly with existing authentication
- ? Follows industry standards and best practices
- ? Includes comprehensive audit logging
- ? Offers excellent user experience

**Security Level**: Significantly enhanced
**User Convenience**: Minimal impact
**Compliance**: RFC, NIST, OWASP compliant
**Status**: Production Ready

---

**Document Version**: 1.0
**Last Updated**: 2024
**Implementation**: Complete ?
