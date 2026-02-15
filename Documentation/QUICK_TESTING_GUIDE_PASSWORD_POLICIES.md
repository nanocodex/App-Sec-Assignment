# Quick Testing Guide - Advanced Account Policies

## ?? Quick Start Testing

### Prerequisites
1. Run the application: `dotnet run`
2. Navigate to: `https://localhost:7XXX`

---

## Test 1: Password History (2-minute test)

**Objective**: Verify users cannot reuse last 2 passwords

**Steps**:
1. Register a new account or login
2. Go to **Security ? Change Password**
3. Change password to: `NewPassword123!`
4. Try changing password again to: `NewPassword123!`
   - ? Should fail with: "You cannot reuse any of your last 2 passwords"
5. Change password to: `AnotherPassword456!`
6. Try changing back to first password: `NewPassword123!`
   - ? Should fail (still in history)
7. Change password to: `ThirdPassword789!` (success)
8. Now try changing to `NewPassword123!`
   - ? Should work (no longer in last 2)

**Expected Result**: Password history prevents reuse of last 2 passwords

---

## Test 2: Minimum Password Age (5-minute test)

**Objective**: Verify users cannot change password too frequently

**Steps**:
1. Login to your account
2. Go to **Security ? Change Password**
3. Change password to: `FirstChange123!` (success)
4. Immediately try to change password again
   - ? Should fail with: "You must wait X more minute(s)"
5. Wait 5+ minutes
6. Try changing password to: `SecondChange456!`
   - ? Should succeed

**Expected Result**: Minimum 5-minute wait enforced between password changes

---

## Test 3: Maximum Password Age (1-minute test)

**Objective**: Verify password expiry forces password change

**Steps**:
1. Logout from application
2. In database, run:
   ```sql
   UPDATE AspNetUsers 
   SET PasswordExpiryDate = DATEADD(day, -1, GETUTCDATE()),
       MustChangePassword = 1
   WHERE Email = 'your-email@example.com'
   ```
3. Try to login
   - ? Should redirect to Change Password page
4. See message: "Your password has expired. You must change it to continue."
5. Change password
   - ? Should succeed and redirect to home page

**Expected Result**: Expired password forces change before access granted

---

## Test 4: Account Lockout & Auto-Recovery (2-minute test)

**Objective**: Verify automatic unlock after lockout period

**Steps**:
1. Go to Login page
2. Enter valid email but **wrong password** 3 times
   - ? After 3rd attempt: "Account locked out... Please try again after 1 minute"
3. Wait exactly 1 minute
4. Try logging in with **correct password**
   - ? Should succeed (automatic unlock)

**Expected Result**: Account auto-unlocks after 1 minute

---

## Test 5: Forgot Password - Email Link (2-minute test)

**Objective**: Verify email-based password reset

**Steps**:
1. Go to Login page
2. Click "Forgot your password?"
3. Enter your email
4. Leave "Reset via SMS" **unchecked**
5. Click "Send Reset Instructions"
   - ? Success message shown
6. Check **application console/logs** for reset link:
   ```
   To: your-email@example.com
   Subject: Password Reset Request
   ```
7. Copy the reset link from logs (format: `/ResetPassword?userId=...&token=...`)
8. Paste in browser
9. Enter new password: `ResetViaEmail123!`
   - ? Should succeed and redirect to login

**Expected Result**: Password reset via email link works

**Note**: In production, check your actual email inbox

---

## Test 6: Forgot Password - SMS Code (2-minute test)

**Objective**: Verify SMS-based password reset

**Steps**:
1. Go to Login page
2. Click "Forgot your password?"
3. Enter your email
4. **Check** "Reset via SMS"
5. Click "Send Reset Instructions"
   - ? Redirects to SMS reset page
6. Check **application console/logs** for 6-digit code:
   ```
   To: 8XXXXXXX
   Message: Your password reset code is: 123456
   ```
7. On SMS reset page:
   - Enter your mobile number (e.g., `81234567`)
   - Enter the 6-digit code from logs
   - Enter new password: `ResetViaSMS456!`
8. Submit
   - ? Should succeed and redirect to login

**Expected Result**: Password reset via SMS code works

**Note**: In production, check your actual SMS inbox

---

## Test 7: Password Strength Indicator (1-minute test)

**Objective**: Verify real-time password strength feedback

**Steps**:
1. Go to **Security ? Change Password**
2. In "New Password" field, type:
   - `weak` - ? Red progress bar, "Weak Password"
   - `Weak12` - ?? Yellow bar, still weak
   - `WeakPass12` - ?? Better but not strong
   - `WeakPass12!` - ? Green bar, "Strong Password"
3. Verify checklist items turn green:
   - ? At least 12 characters
   - ? Lowercase letter
   - ? Uppercase letter
   - ? Number
   - ? Special character

**Expected Result**: Live feedback helps users create strong passwords

---

## Test 8: Email Notifications (1-minute test)

**Objective**: Verify password change notification emails

**Steps**:
1. Change your password via **Security ? Change Password**
2. Check **application console/logs** for notification email:
   ```
   To: your-email@example.com
   Subject: Password Changed Successfully
   Body: Your password has been changed successfully...
   ```

**Expected Result**: Notification email sent after password change

---

## Test 9: Audit Logging (1-minute test)

**Objective**: Verify all password activities are logged

**Steps**:
1. Login to application
2. Go to **Security ? Audit Logs**
3. Look for these activity types:
   - "Password Changed"
   - "Password Reset Requested - Email"
   - "Password Reset Success"
   - "Password Change Failed - Password Reused"
   - "Password Change Failed - Too Soon"
   - "Login - Password Change Required"
4. Verify each log contains:
   - ? Timestamp
   - ? Activity type
   - ? Description
   - ? IP address
   - ? User agent

**Expected Result**: Complete audit trail of password activities

---

## Test 10: Password Expiry Warning (Quick Check)

**Objective**: Verify users are warned before password expires

**Steps**:
1. In database, set expiry to 6 days from now:
   ```sql
   UPDATE AspNetUsers 
   SET PasswordExpiryDate = DATEADD(day, 6, GETUTCDATE())
   WHERE Email = 'your-email@example.com'
   ```
2. Go to **Security ? Change Password**
3. Look for warning message:
   - ? "Your password will expire in 6 day(s). Please change it soon."

**Expected Result**: Early warning shown when expiry is near

---

## ?? Database Verification Queries

### Check Password History
```sql
SELECT u.Email, ph.CreatedAt, ph.PasswordHash
FROM PasswordHistories ph
JOIN AspNetUsers u ON ph.UserId = u.Id
ORDER BY ph.CreatedAt DESC
```

### Check Password Expiry Dates
```sql
SELECT Email, LastPasswordChangeDate, PasswordExpiryDate, MustChangePassword,
       DATEDIFF(day, GETUTCDATE(), PasswordExpiryDate) AS DaysUntilExpiry
FROM AspNetUsers
WHERE Email IS NOT NULL
```

### Check Reset Tokens
```sql
SELECT u.Email, prt.Token, prt.CreatedAt, prt.ExpiresAt, prt.IsUsed
FROM PasswordResetTokens prt
JOIN AspNetUsers u ON prt.UserId = u.Id
ORDER BY prt.CreatedAt DESC
```

### Check Lockout Status
```sql
SELECT Email, AccessFailedCount, LockoutEnd, LockoutEnabled
FROM AspNetUsers
WHERE Email IS NOT NULL
```

---

## ? Quick Test Checklist

- [ ] Password history prevents reuse (Test 1)
- [ ] Minimum password age enforced (Test 2)
- [ ] Maximum password age enforced (Test 3)
- [ ] Account lockout auto-recovers (Test 4)
- [ ] Email reset link works (Test 5)
- [ ] SMS reset code works (Test 6)
- [ ] Password strength indicator works (Test 7)
- [ ] Email notifications sent (Test 8)
- [ ] Audit logs created (Test 9)
- [ ] Expiry warnings shown (Test 10)

---

## ?? Expected Configuration (from appsettings.json)

```json
"PasswordPolicy": {
  "HistoryCount": 2,
  "MinPasswordAgeMinutes": 5,
  "MaxPasswordAgeDays": 90
}
```

**Lockout Settings** (from Program.cs):
- DefaultLockoutTimeSpan: 1 minute
- MaxFailedAccessAttempts: 3 attempts

---

## ?? Test Passwords to Use

**Strong passwords that meet all requirements:**
- `TestPassword123!`
- `NewPassword456!`
- `AnotherPass789!`
- `SecurePass000!`
- `FreshPassword111!`

**Weak passwords for testing validation:**
- `weak` - Too short
- `WeakPassword` - No number or special char
- `weakpass123!` - No uppercase
- `WEAKPASS123!` - No lowercase

---

## ? Success Criteria

**All tests pass if:**
1. ? Build successful (no errors)
2. ? Database migration applied
3. ? All pages render without errors
4. ? Password history working (prevents reuse)
5. ? Minimum age working (5-minute wait)
6. ? Maximum age working (90-day expiry)
7. ? Account lockout auto-recovers (1 minute)
8. ? Email reset works (link sent and accepted)
9. ? SMS reset works (code sent and validated)
10. ? Audit logs created for all activities

---

## ?? Common Issues & Solutions

### "Email sent" but no email in inbox
- **Cause**: SMTP not configured
- **Solution**: Check application logs for email content
- **Production**: Configure SMTP in appsettings.json

### "SMS sent" but no SMS received
- **Cause**: SMS provider not configured
- **Solution**: Check application logs for SMS content
- **Production**: Configure SMS provider (e.g., Twilio)

### Reset token expired
- **Cause**: Tokens expire after 1 hour
- **Solution**: Request new reset link

### SMS code expired
- **Cause**: Codes expire after 10 minutes
- **Solution**: Request new reset code

### Cannot change password (too soon)
- **Cause**: Minimum password age enforced
- **Solution**: Wait 5 minutes or set MustChangePassword = true

---

**Testing Time**: ~15 minutes for complete test suite
**Status**: Ready for testing! ??
