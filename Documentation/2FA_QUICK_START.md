# 2FA Quick Start Guide

## For Users: How to Set Up 2FA in 5 Minutes

### Step 1: Install an Authenticator App (2 minutes)

**Choose one:**
- **Google Authenticator** (Recommended for beginners)
  - iOS: https://apps.apple.com/app/google-authenticator/id388497605
  - Android: https://play.google.com/store/apps/details?id=com.google.android.apps.authenticator2

- **Microsoft Authenticator** (Recommended for Microsoft users)
  - iOS: https://apps.apple.com/app/microsoft-authenticator/id983156458
  - Android: https://play.google.com/store/apps/details?id=com.azure.authenticator

- **Authy** (Multi-device support)
  - iOS/Android/Desktop: https://authy.com/download/

### Step 2: Enable 2FA on Your Account (3 minutes)

1. **Login** to your account
2. **Navigate** to `/Enable2FA` or find "Enable 2FA" in settings
3. **Open** your authenticator app
4. **Tap** the "+" or "Add Account" button
5. **Scan** the QR code displayed on screen
6. **Enter** the 6-digit code shown in your app
7. **Click** "Verify and Enable 2FA"
8. **Save** your recovery codes! (Print or download)

**? Done!** Your account is now protected with 2FA.

---

## Quick Reference

### Login with 2FA
1. Enter email and password
2. Enter 6-digit code from authenticator app
3. (Optional) Check "Remember this device" for 30 days
4. Click "Verify"

### Use Recovery Code (Lost Device)
1. On 2FA verification page, click "Use a recovery code instead"
2. Enter one of your saved recovery codes
3. Click "Use Recovery Code"
4. **Generate new codes immediately after login!**

### Manage 2FA
- **View Status**: Go to `/Manage2FA`
- **Generate New Recovery Codes**: Click button on Manage 2FA page
- **Disable 2FA**: Click "Disable" button (requires confirmation)

---

## For Developers: Testing 2FA

### Quick Test Checklist

**Test 1: Enable 2FA** ?
```
1. Login ? /Enable2FA
2. Install Google Authenticator
3. Scan QR code
4. Enter code ? Should succeed
5. Check recovery codes displayed
```

**Test 2: Login with 2FA** ?
```
1. Logout ? Login with credentials
2. Should redirect to /Verify2FA
3. Enter code from app ? Should login
4. Check audit log for "2FA Login Success"
```

**Test 3: Recovery Code** ?
```
1. Logout ? Login with credentials
2. Click "Use a recovery code instead"
3. Enter recovery code ? Should login
4. Check warning if ?3 codes left
```

**Test 4: Invalid Code** ?
```
1. Logout ? Login
2. Enter wrong code (000000)
3. Should show error
4. Check audit log for failed attempt
```

**Test 5: Manage Page** ?
```
1. Login ? /Manage2FA
2. Check status shown correctly
3. Test "Generate New Codes"
4. Test "Disable 2FA"
```

### Verify Database
```sql
-- Check 2FA status
SELECT Email, TwoFactorEnabled, IsTwoFactorEnabled,
       CASE WHEN TwoFactorRecoveryCodes IS NULL THEN 'No' ELSE 'Yes' END as HasRecoveryCodes
FROM AspNetUsers

-- Check authenticator keys
SELECT u.Email, t.Name, LEN(t.Value) as KeyLength
FROM AspNetUsers u
JOIN AspNetUserTokens t ON u.Id = t.UserId
WHERE t.Name = 'AuthenticatorKey'

-- Check audit logs
SELECT TOP 10 Action, Timestamp, Details
FROM AuditLogs
WHERE Action LIKE '%2FA%'
ORDER BY Timestamp DESC
```

---

## Architecture Overview

### Flow Diagram
```
Enable 2FA:
  User ? /Enable2FA ? Generate Secret ? Show QR ? Verify Code ? Save Key ? Show Recovery Codes

Login with 2FA:
  User ? /Login ? Verify Password ? /Verify2FA ? Verify TOTP ? Create Session ? Homepage

Recovery Code:
  User ? /Verify2FA ? Click "Recovery" ? Enter Code ? Remove Code ? Login
```

### Key Components
- **TwoFactorService**: TOTP generation and verification
- **Enable2FA Page**: Enrollment wizard
- **Verify2FA Page**: Login verification
- **Manage2FA Page**: Settings management
- **ApplicationUser**: Stores 2FA status and recovery codes
- **AspNetUserTokens**: Stores authenticator keys

---

## Common Issues & Solutions

### ? "Code always invalid"
**Solution**: Check device time is set to automatic (Settings ? Date & Time)

### ? "QR code won't scan"
**Solution**: Use manual entry (text code shown below QR code)

### ? "Lost recovery codes"
**Solution**: 
- If you have device: Login ? Manage2FA ? Generate New Codes
- If lost device: Contact admin for account recovery

### ? "Want to reset 2FA"
**Solution**: Login ? Manage2FA ? Disable ? Enable again (new setup)

---

## Security Best Practices

### ? DO:
- Save recovery codes in a password manager
- Print recovery codes and store securely
- Enable "Remember this device" on trusted devices
- Generate new recovery codes after using one
- Check audit logs regularly

### ? DON'T:
- Share your QR code or secret key
- Take screenshots of QR code or recovery codes
- Store recovery codes in email or cloud unencrypted
- Disable 2FA unless absolutely necessary
- Reuse recovery codes (they're one-time use)

---

## Support

### For Users:
- **Enable 2FA**: `/Enable2FA`
- **Manage 2FA**: `/Manage2FA`
- **View Audit Logs**: `/AuditLogs`

### For Developers:
- **Full Documentation**: `2FA_IMPLEMENTATION_GUIDE.md`
- **Code**: `Services/TwoFactorService.cs`
- **Database**: Check AspNetUsers and AspNetUserTokens tables

---

## Quick Stats

- **Time to Enable**: ~3 minutes
- **Code Length**: 6 digits
- **Code Refresh**: Every 30 seconds
- **Recovery Codes**: 10 per user
- **Remember Device**: 30 days
- **Encryption**: AES-256 (recovery codes)
- **Algorithm**: TOTP (RFC 6238)

---

**Status**: ? 2FA Fully Implemented and Ready to Use
**Last Updated**: 2024
