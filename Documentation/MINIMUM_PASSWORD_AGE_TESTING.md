# Minimum Password Age - Testing Guide

## Quick Test: 1-Minute Password Age Policy

This guide shows you how to test the **1-minute minimum password age** enforcement.

---

## Prerequisites

? Application is running  
? `MinPasswordAgeMinutes` is set to `1` in `appsettings.json`  
? Build is successful

---

## Test Scenario: Rapid Password Change Attempt

### Step 1: Register a New User

1. Navigate to `/Register`
2. Fill in all required fields:
   - First Name: Test
   - Last Name: User
   - Email: testuser@example.com
   - Mobile: 81234567
   - Credit Card: 4111 1111 1111 1111
   - Billing: 123 Test Street
   - Shipping: 123 Test Street
   - Password: MyP@ssw0rd123
   - Confirm Password: MyP@ssw0rd123
   - Photo: Upload any JPG file
3. Click "Register"

**Expected Result:**
```
? Registration successful
? Automatically logged in
? Redirected to homepage
```

**Database Check:**
```sql
SELECT 
    Email,
    LastPasswordChangeDate,
    PasswordExpiryDate,
    DATEDIFF(SECOND, LastPasswordChangeDate, GETUTCDATE()) AS SecondsSinceChange
FROM AspNetUsers
WHERE Email = 'testuser@example.com';
```

**Expected:**
- `LastPasswordChangeDate`: Current UTC time
- `PasswordExpiryDate`: 90 days from now
- `SecondsSinceChange`: ~0-5 seconds

---

### Step 2: Immediately Try to Change Password (Should Fail)

1. Navigate to `/ChangePassword`
2. Enter:
   - Current Password: MyP@ssw0rd123
   - New Password: NewP@ssw0rd456
   - Confirm New Password: NewP@ssw0rd456
3. Click "Change Password"

**Expected Result:**
```
? Error: "You must wait 1 more minute(s) before changing your password again."
?? Red alert box with error message
?? No password change occurred
```

**Audit Log Check:**
```sql
SELECT TOP 5 * FROM AuditLogs 
WHERE UserId = (SELECT Id FROM AspNetUsers WHERE Email = 'testuser@example.com')
ORDER BY Timestamp DESC;
```

**Expected:**
```
Action: Password Change Failed - Too Soon
Details: You must wait 1 more minute(s) before changing your password again.
```

---

### Step 3: Wait 61 Seconds, Then Try Again (Should Succeed)

1. **Wait 61 seconds** (use a timer)
2. Navigate to `/ChangePassword` (or refresh page)
3. Enter:
   - Current Password: MyP@ssw0rd123
   - New Password: NewP@ssw0rd456
   - Confirm New Password: NewP@ssw0rd456
4. Click "Change Password"

**Expected Result:**
```
? Success: "Your password has been changed successfully."
? Redirected to homepage
? Password updated in database
```

**Audit Log Check:**
```sql
SELECT TOP 5 * FROM AuditLogs 
WHERE UserId = (SELECT Id FROM AspNetUsers WHERE Email = 'testuser@example.com')
ORDER BY Timestamp DESC;
```

**Expected:**
```
Action: Password Changed
Details: Password changed successfully from {IP}
```

---

## Test Scenario: Admin-Forced Password Change (Bypass)

This tests that users with `MustChangePassword = true` can bypass the minimum age policy.

### Step 1: Set MustChangePassword Flag

```sql
UPDATE AspNetUsers 
SET MustChangePassword = 1 
WHERE Email = 'testuser@example.com';
```

### Step 2: Login

1. Logout
2. Login with: testuser@example.com / NewP@ssw0rd456
3. Should be **automatically redirected** to `/ChangePassword`
4. Message: "Your password has expired. You must change it to continue."

### Step 3: Change Password Immediately

1. Enter:
   - Current Password: NewP@ssw0rd456
   - New Password: AdminForced@789
   - Confirm New Password: AdminForced@789
2. Click "Change Password"

**Expected Result:**
```
? Success: Password changed successfully (minimum age bypassed)
? MustChangePassword flag automatically reset to false
? User can now access application
```

---

## Test Scenario: Password History Prevention

This tests that the minimum age works together with password history.

### Setup

Ensure you have changed password at least once (from previous test).

### Test

1. Wait 61 seconds after last password change
2. Navigate to `/ChangePassword`
3. Try to change back to old password: MyP@ssw0rd123

**Expected Result:**
```
? Error: "You cannot reuse any of your last 2 passwords. Please choose a different password."
?? Blocked by password history, NOT minimum age
```

---

## Verification Queries

### Check Password Change Dates

```sql
SELECT 
    Email,
    LastPasswordChangeDate,
    PasswordExpiryDate,
    MustChangePassword,
    DATEDIFF(SECOND, LastPasswordChangeDate, GETUTCDATE()) AS SecondsSinceLastChange,
    CASE 
        WHEN MustChangePassword = 1 THEN 'CAN CHANGE (Forced)'
        WHEN DATEDIFF(SECOND, LastPasswordChangeDate, GETUTCDATE()) < 60 THEN 'TOO SOON (< 1 min)'
        ELSE 'CAN CHANGE (? 1 min)'
    END AS PasswordChangeStatus
FROM AspNetUsers
WHERE Email = 'testuser@example.com';
```

### Check Password History

```sql
SELECT 
    ph.CreatedAt,
    DATEDIFF(SECOND, ph.CreatedAt, GETUTCDATE()) AS SecondsAgo,
    ph.PasswordHash
FROM PasswordHistories ph
JOIN AspNetUsers u ON ph.UserId = u.Id
WHERE u.Email = 'testuser@example.com'
ORDER BY ph.CreatedAt DESC;
```

**Expected:** Should see 2 password hashes (original + one change)

### Check Audit Log

```sql
SELECT 
    Action,
    Timestamp,
    Details,
    IpAddress
FROM AuditLogs
WHERE UserId = (SELECT Id FROM AspNetUsers WHERE Email = 'testuser@example.com')
ORDER BY Timestamp DESC;
```

**Expected Actions:**
1. Password Changed (success)
2. Password Change Failed - Too Soon
3. Registration

---

## Quick Reference: Expected Behavior

| Scenario | Time Since Last Change | Expected Result |
|----------|------------------------|-----------------|
| Just registered | 0 seconds | ? Too soon |
| 30 seconds later | 30 seconds | ? Too soon |
| 59 seconds later | 59 seconds | ? Too soon |
| 60 seconds later | 60 seconds | ? Too soon (< 60 exactly) |
| 61 seconds later | 61 seconds | ? Allowed |
| 2 minutes later | 120 seconds | ? Allowed |
| MustChangePassword = true | Any time | ? Allowed (bypasses rule) |

---

## Error Messages to Expect

### Minimum Age Not Met
```
You must wait 1 more minute(s) before changing your password again.
```

### Password History Violation
```
You cannot reuse any of your last 2 passwords. Please choose a different password.
```

### Current Password Incorrect
```
Current password is incorrect.
```

### Password Complexity Not Met
```
Password must contain at least 12 characters.
Password must contain at least one uppercase letter (A-Z).
(etc.)
```

---

## Common Issues

### Issue: Error message shows "0 more minute(s)"
**Reason:** Time calculation is using `Math.Ceiling()` which rounds up  
**Solution:** Wait a few more seconds, the message updates on next attempt

### Issue: Can change password immediately after registration
**Possible Causes:**
1. `LastPasswordChangeDate` not set during registration
2. `MinPasswordAgeMinutes` set to 0 in config
3. `MustChangePassword` flag is true

**Check:**
```sql
SELECT LastPasswordChangeDate, MustChangePassword 
FROM AspNetUsers 
WHERE Email = 'testuser@example.com';
```

### Issue: Always get "Too Soon" error, even after waiting
**Possible Causes:**
1. Server time vs. database time mismatch
2. Application restarted (user object cached?)

**Solution:**
```sql
-- Check server time
SELECT GETUTCDATE() AS ServerTime;

-- Check user's last change date
SELECT LastPasswordChangeDate FROM AspNetUsers WHERE Email = 'testuser@example.com';
```

---

## Configuration Changes (Optional)

### Change Minimum Age to 5 Minutes

**File:** `appsettings.json`

```json
"PasswordPolicy": {
  "MinPasswordAgeMinutes": 5
}
```

**Restart application** for changes to take effect.

### Change to 1 Day (1440 Minutes)

```json
"PasswordPolicy": {
  "MinPasswordAgeMinutes": 1440
}
```

---

## Production Recommendations

For production environments:

| Setting | Recommended Value | Reason |
|---------|-------------------|--------|
| **Development/Testing** | 1 minute | Quick testing |
| **Staging** | 60 minutes (1 hour) | Realistic testing |
| **Production** | 1440 minutes (1 day) | Security best practice |
| **High Security** | 10080 minutes (1 week) | Prevent password cycling |

---

## Summary

? **Minimum password age is enforced at: 1 minute**  
? **Configured in:** `appsettings.json` ? `PasswordPolicy:MinPasswordAgeMinutes`  
? **Enforced in:** `PasswordManagementService.CanChangePasswordAsync()`  
? **Called from:** `ChangePassword.cshtml.cs` before allowing password change  
? **Bypassed when:** `MustChangePassword = true` (admin-forced change)  
? **Logged in:** AuditLogs table with action "Password Change Failed - Too Soon"  

**Test completed successfully! ?**
