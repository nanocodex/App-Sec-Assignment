# Minimum Password Age Implementation

## Overview
This document describes the implementation of the **minimum password age policy** that prevents users from changing their password too frequently.

---

## Configuration

### Location: `appsettings.json`

```json
"PasswordPolicy": {
  "HistoryCount": 2,
  "MinPasswordAgeMinutes": 1,
  "MaxPasswordAgeDays": 90
}
```

### Settings:

| Setting | Value | Description |
|---------|-------|-------------|
| `MinPasswordAgeMinutes` | **1 minute** | Minimum time that must pass before user can change password again |
| `MaxPasswordAgeDays` | 90 days | Maximum password age before forced change |
| `HistoryCount` | 2 passwords | Number of previous passwords that cannot be reused |

---

## Implementation

### 1. Service Layer

**File:** `Services/PasswordManagementService.cs`

```csharp
public async Task<(bool CanChange, string Message)> CanChangePasswordAsync(string userId)
{
    var user = await _userManager.FindByIdAsync(userId);
    if (user == null)
        return (false, "User not found.");

    // Check if user must change password (override minimum age)
    if (user.MustChangePassword)
        return (true, string.Empty);

    // Check minimum password age
    if (user.LastPasswordChangeDate.HasValue)
    {
        var timeSinceLastChange = DateTime.UtcNow - user.LastPasswordChangeDate.Value;
        var minAge = TimeSpan.FromMinutes(_minPasswordAgeMinutes);

        if (timeSinceLastChange < minAge)
        {
            var remainingTime = minAge - timeSinceLastChange;
            var message = $"You must wait {Math.Ceiling(remainingTime.TotalMinutes)} more minute(s) before changing your password again.";
            return (false, message);
        }
    }

    return (true, string.Empty);
}
```

**Key Points:**
- ? Reads `MinPasswordAgeMinutes` from configuration (default: 5 if not set)
- ? Actual value in config: **1 minute**
- ? Exception: Users with `MustChangePassword = true` can bypass minimum age (admin-forced change)
- ? Calculates time remaining and provides user-friendly message

---

### 2. Page Layer

**File:** `Pages/ChangePassword.cshtml.cs`

```csharp
public async Task<IActionResult> OnPostAsync()
{
    // ... validation code ...

    // Check if user can change password (minimum age policy)
    var (canChange, message) = await _passwordManagement.CanChangePasswordAsync(user.Id);
    if (!canChange)
    {
        ModelState.AddModelError(string.Empty, message);
        await _auditService.LogActivityAsync(
            user.Id,
            "Password Change Failed - Too Soon",
            message,
            ipAddress,
            userAgent);
        return Page();
    }

    // ... proceed with password change ...
}
```

**Key Points:**
- ? Checks minimum age **before** verifying current password
- ? Displays error message to user
- ? Logs failed attempt in audit trail
- ? Prevents password change if minimum age not met

---

## How It Works

### User Flow

```
User attempts to change password
    ?
Check: Can user change password?
    ?
    ??? MustChangePassword = true?
    ?   ??? YES ? Allow change (bypass minimum age)
    ?
    ??? LastPasswordChangeDate exists?
        ??? NO ? Allow change (first password change)
        ?
        ??? YES ? Calculate time since last change
            ??? Time < 1 minute?
            ?   ??? YES ? Block change, show error message
            ?
            ??? Time ? 1 minute?
                ??? YES ? Allow change
```

---

## Testing the Implementation

### Test Scenario 1: Change Password Immediately After Registration

**Steps:**
1. Register a new user
2. Immediately go to `/ChangePassword`
3. Enter current password and new password
4. Click "Change Password"

**Expected Result:**
```
? Error: "You must wait 1 more minute(s) before changing your password again."
```

**Audit Log Entry:**
```
Action: Password Change Failed - Too Soon
Details: You must wait 1 more minute(s) before changing your password again.
```

---

### Test Scenario 2: Change Password After Waiting 1 Minute

**Steps:**
1. Register a new user
2. Wait **1 minute**
3. Go to `/ChangePassword`
4. Enter current password and new password
5. Click "Change Password"

**Expected Result:**
```
? Success: "Your password has been changed successfully."
```

**Audit Log Entry:**
```
Action: Password Changed
Details: Password changed successfully from {IP}
```

---

### Test Scenario 3: Admin-Forced Password Change (Bypass Minimum Age)

**Steps:**
1. Admin sets `MustChangePassword = true` for user in database
2. User logs in
3. Redirected to `/ChangePassword`
4. User changes password immediately

**Expected Result:**
```
? Success: Password change allowed (bypasses minimum age policy)
```

**Reason:** When `MustChangePassword = true`, the policy is bypassed to allow admin-forced changes.

---

### Test Scenario 4: Rapid Password Change Attempts

**Steps:**
1. User changes password (Day 1, 10:00:00)
2. Wait 30 seconds
3. User tries to change password again (Day 1, 10:00:30)
4. Blocked with error message
5. Wait another 31 seconds (total: 61 seconds)
6. User tries to change password again (Day 1, 10:01:01)

**Expected Results:**
```
Attempt 1 (10:00:00): ? Success
Attempt 2 (10:00:30): ? Blocked - "You must wait 1 more minute(s)..."
Attempt 3 (10:01:01): ? Success (1+ minute has passed)
```

---

## Database Verification

### Check Last Password Change Date

```sql
SELECT 
    Email,
    LastPasswordChangeDate,
    PasswordExpiryDate,
    MustChangePassword,
    DATEDIFF(MINUTE, LastPasswordChangeDate, GETUTCDATE()) AS MinutesSinceLastChange,
    CASE 
        WHEN MustChangePassword = 1 THEN 'MUST CHANGE (Bypasses Min Age)'
        WHEN DATEDIFF(MINUTE, LastPasswordChangeDate, GETUTCDATE()) < 1 THEN 'TOO SOON (< 1 min)'
        ELSE 'CAN CHANGE (? 1 min)'
    END AS PasswordChangeStatus
FROM AspNetUsers
ORDER BY LastPasswordChangeDate DESC;
```

**Example Output:**
```
Email               | LastPasswordChangeDate   | MinutesSinceLastChange | PasswordChangeStatus
--------------------|--------------------------|------------------------|--------------------
user@example.com    | 2024-02-20 14:30:00      | 0                      | TOO SOON (< 1 min)
olduser@example.com | 2024-02-20 14:25:00      | 5                      | CAN CHANGE (? 1 min)
admin@example.com   | 2024-02-20 14:00:00      | 30                     | CAN CHANGE (? 1 min)
```

---

## Security Benefits

### Why Enforce Minimum Password Age?

1. **Prevents Password History Circumvention**
   - Users cannot rapidly cycle through passwords to reuse an old one
   - Example: Without min age, user could change password 3 times in a row to bypass 2-password history

2. **Reduces Administrative Burden**
   - Prevents users from frequently requesting password resets
   - Limits password change-related support tickets

3. **Enforces Password Discipline**
   - Encourages users to choose strong passwords they can remember
   - Discourages frequent password changes due to forgetfulness

4. **Compliance with Security Policies**
   - Many security frameworks (NIST, ISO 27001) recommend minimum password age
   - Demonstrates security best practices

---

## Configuration Options

### To Change Minimum Age

**File:** `appsettings.json`

```json
"PasswordPolicy": {
  "MinPasswordAgeMinutes": 1440  // Change to 1 day (1440 minutes)
}
```

**Common Values:**

| Value | Time Period | Use Case |
|-------|-------------|----------|
| 1 | 1 minute | **Testing/Demo** (current setting) |
| 60 | 1 hour | Development environment |
| 1440 | 1 day | Standard production setting |
| 10080 | 1 week | High-security environment |

---

## Exception Handling

### When Minimum Age is Bypassed

1. **Admin-Forced Password Change**
   - `MustChangePassword = true`
   - User must change password on next login
   - Minimum age policy is **ignored**

2. **First Password Change**
   - `LastPasswordChangeDate = null`
   - User has never changed password
   - No minimum age applies

3. **Password Expiry**
   - `PasswordExpiryDate < UtcNow`
   - Password has expired (90 days)
   - User is **forced** to change
   - Minimum age is **ignored** via `MustChangePassword` flag

---

## Audit Logging

All password change attempts are logged:

### Successful Change
```json
{
  "UserId": "abc-123",
  "Action": "Password Changed",
  "Timestamp": "2024-02-20T14:30:00Z",
  "IpAddress": "192.168.1.100",
  "Details": "Password changed successfully from 192.168.1.100"
}
```

### Blocked by Minimum Age
```json
{
  "UserId": "abc-123",
  "Action": "Password Change Failed - Too Soon",
  "Timestamp": "2024-02-20T14:30:30Z",
  "IpAddress": "192.168.1.100",
  "Details": "You must wait 1 more minute(s) before changing your password again."
}
```

---

## User Experience

### Error Message Display

**File:** `Pages/ChangePassword.cshtml`

When user tries to change password too soon:

```html
<div class="alert alert-danger">
    <i class="bi bi-exclamation-triangle"></i>
    You must wait 1 more minute(s) before changing your password again.
</div>
```

**Message Format:**
- Clear and specific
- Tells user exactly how long to wait
- Calculated in real-time based on remaining time

---

## Summary

### ? Implementation Status: COMPLETE

| Feature | Status | Details |
|---------|--------|---------|
| **Minimum Password Age** | ? Implemented | 1 minute enforced |
| **Configuration** | ? Set | `appsettings.json` ? `MinPasswordAgeMinutes: 1` |
| **Validation Logic** | ? Working | `CanChangePasswordAsync()` method |
| **Page Enforcement** | ? Active | `ChangePassword.cshtml.cs` checks before allowing change |
| **Error Messages** | ? User-Friendly | Displays time remaining |
| **Audit Logging** | ? Enabled | Logs all attempts |
| **Exception Handling** | ? Implemented | Bypasses for forced changes |

---

## Quick Reference

**To change minimum password age:**
1. Open `appsettings.json`
2. Find `"PasswordPolicy"` section
3. Change `"MinPasswordAgeMinutes"` value
4. Restart application
5. Changes take effect immediately

**Current Setting:** **1 minute**

**Recommended for Production:** **1440 minutes (1 day)**

---

## Related Documentation

- [PASSWORD_MANAGEMENT_GUIDE.md](./PASSWORD_MANAGEMENT_GUIDE.md) - Complete password management system
- [DATA_SECURITY_IMPLEMENTATION_SUMMARY.md](./DATA_SECURITY_IMPLEMENTATION_SUMMARY.md) - Password protection overview
- [CREDENTIAL_VERIFICATION_IMPLEMENTATION.md](./CREDENTIAL_VERIFICATION_IMPLEMENTATION.md) - Login and authentication

---

**Implementation Date:** February 2024  
**Framework:** ASP.NET Core 8 Razor Pages  
**Status:** Production Ready ?
