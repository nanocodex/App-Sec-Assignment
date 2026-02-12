# Session Timeout Configuration - 1 Minute

## Overview
Session timeout has been configured to **1 minute of inactivity** for enhanced security. Users will be automatically redirected to the login page with a clear notification when their session expires.

---

## Configuration Changes

### 1. Session Timeout - Program.cs

**ASP.NET Core Session:**
```csharp
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(1);  // Changed from 30 to 1 minute
    // ... other settings
});
```

**Authentication Cookie:**
```csharp
builder.Services.ConfigureApplicationCookie(options =>
{
    options.ExpireTimeSpan = TimeSpan.FromMinutes(1);  // Changed from 30 to 1 minute
    options.SlidingExpiration = true;  // Timer resets on each request
    // ... other settings
});
```

### 2. Database Session Timeout - SessionService.cs

```csharp
private readonly TimeSpan _sessionTimeout = TimeSpan.FromMinutes(1);  // Changed from 30 to 1 minute
```

---

## User Experience

### Session Expiration Flow

1. **User logs in** ? Session starts with 1-minute timer
2. **User navigates pages** ? Timer resets on each action (sliding expiration)
3. **User idle for 1 minute** ? Session expires
4. **User tries to access any page** ? Middleware detects expired session
5. **Auto-redirect to login** ? URL: `/Login?timeout=true`
6. **Timeout notification displayed**:
   ```
   ?? Session Expired: Your session has expired. Please log in again.
   ```

### Login Page Changes

**Timeout Message:**
- Displayed prominently when `?timeout=true` parameter is present
- Styled as a warning alert with dismiss button
- Message: "Session Expired: Your session has expired. Please log in again."

**Session Timeout Notice:**
- New warning alert added to login page
- Message: "Session Timeout: Your session will expire after 1 minute of inactivity for security purposes."
- Informs users upfront about the timeout policy

---

## Files Modified

1. ? **Program.cs**
   - Session timeout: 30 min ? 1 min
   - Authentication cookie timeout: 30 min ? 1 min

2. ? **Services/SessionService.cs**
   - Database session timeout: 30 min ? 1 min

3. ? **Pages/Login.cshtml**
   - Enhanced timeout message display
   - Added session timeout warning notice

4. ? **Pages/ActiveSessions.cshtml**
   - Updated session info: 30 min ? 1 min
   - Added auto-redirect notice

---

## Testing the Timeout

### Quick Test (1 Minute)

1. **Login to the application**
   ```
   Navigate to /Login
   Enter credentials
   Submit form
   ```

2. **Navigate to any page**
   ```
   Go to /Index or /Privacy
   ```

3. **Wait 61 seconds** (1 minute + buffer)
   - Do NOT interact with the page
   - Do NOT click anything
   - Do NOT refresh

4. **Try to navigate to another page**
   ```
   Click any link or button
   ```

5. **Expected Result:**
   - ? Automatically redirected to `/Login?timeout=true`
   - ? Warning message displayed: "Session Expired: Your session has expired. Please log in again."
   - ? Session timeout notice shown: "Your session will expire after 1 minute of inactivity"
   - ? User can log in again immediately

### Browser Developer Tools Test

1. Login and open Developer Tools (F12)
2. Go to Application/Storage tab
3. View Cookies
4. Look for `.AspNetCore.Identity.Application` cookie
5. Note the expiration time
6. Wait 1 minute without any activity
7. Try to access a page
8. Cookie should be expired/removed

---

## Sliding Expiration Explained

**How it works:**
- Timer **resets** on every page request/action
- If user clicks around, session stays alive
- Only expires after 1 continuous minute of **no activity**

**Example Timeline:**
```
00:00 - User logs in                    ? Timer starts (expires at 01:00)
00:30 - User clicks a link              ? Timer resets (now expires at 01:30)
00:45 - User clicks another link        ? Timer resets (now expires at 01:45)
01:00 - User idle (no activity)         ? Still active (expires at 01:45)
01:45 - User still idle                 ? Session expires
01:46 - User clicks link                ? Redirected to login with timeout message
```

---

## Database Session Records

### Check Session Expiration Times

```sql
SELECT 
    u.Email,
    s.SessionId,
    s.CreatedAt,
    s.LastActivityAt,
    s.ExpiresAt,
    s.IsActive,
    DATEDIFF(SECOND, GETUTCDATE(), s.ExpiresAt) AS SecondsUntilExpiration,
    CASE 
        WHEN s.ExpiresAt > GETUTCDATE() AND s.IsActive = 1 THEN 'ACTIVE'
        WHEN s.ExpiresAt <= GETUTCDATE() THEN 'EXPIRED'
        ELSE 'TERMINATED'
    END AS Status
FROM UserSessions s
INNER JOIN AspNetUsers u ON s.UserId = u.Id
WHERE s.IsActive = 1
ORDER BY s.ExpiresAt DESC;
```

**Expected Results:**
- Active sessions should show `SecondsUntilExpiration` ? 60 seconds
- Expired sessions should show `Status = 'EXPIRED'`

---

## Middleware Validation

The **SessionValidationMiddleware** runs on every request and:

1. Checks if user is authenticated
2. Gets session ID from HttpContext.Session
3. Validates session against database
4. Checks if session has expired (ExpiresAt < UtcNow)
5. If expired or invalid:
   - Signs out user
   - Clears session
   - Redirects to `/Login?timeout=true`

**Code Location:** `Middleware/SessionValidationMiddleware.cs`

---

## Timeout Notification Details

### Login Page Display

**Timeout Alert:**
```html
<div class="alert alert-warning alert-dismissible fade show">
    <i class="bi bi-clock-history"></i> 
    <strong>Session Expired:</strong> Your session has expired. Please log in again.
    <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
</div>
```

**Session Timeout Notice:**
```html
<div class="alert alert-warning">
    <small>
        <i class="bi bi-exclamation-triangle"></i>
        <strong>Session Timeout:</strong> Your session will expire after 1 minute 
        of inactivity for security purposes.
    </small>
</div>
```

### Styling
- Warning color (yellow/orange) to draw attention
- Bootstrap Icons for visual clarity
- Dismissible for user convenience
- Responsive design

---

## Security Benefits

### 1-Minute Timeout Advantages

1. **Enhanced Security**
   - Reduces window for session hijacking
   - Minimizes risk if device left unattended
   - Forces frequent re-authentication

2. **Compliance**
   - Meets strict security requirements
   - Suitable for high-security environments
   - Demonstrates security-first approach

3. **User Protection**
   - Protects on shared computers
   - Auto-logout from public terminals
   - Prevents unauthorized access

### Trade-offs

**Pros:**
- ? Maximum security
- ? Automatic logout from unattended sessions
- ? Clear user notifications

**Cons:**
- ?? Frequent re-login required
- ?? May interrupt user workflow
- ?? Not suitable for long-form data entry

---

## Production Considerations

### Adjusting Timeout (If Needed)

**For Development/Testing:**
```csharp
// 1 minute for quick testing
options.IdleTimeout = TimeSpan.FromMinutes(1);
```

**For Production (if 1 min too short):**
```csharp
// 5 minutes - balance of security and usability
options.IdleTimeout = TimeSpan.FromMinutes(5);

// 15 minutes - standard for most applications
options.IdleTimeout = TimeSpan.FromMinutes(15);

// 30 minutes - less strict, more user-friendly
options.IdleTimeout = TimeSpan.FromMinutes(30);
```

**Remember to update ALL THREE locations:**
1. `Program.cs` - Session configuration
2. `Program.cs` - Authentication cookie configuration  
3. `Services/SessionService.cs` - Database session timeout

### Environment-Based Configuration

**Recommended approach:**
```csharp
var sessionTimeout = builder.Environment.IsDevelopment() 
    ? TimeSpan.FromMinutes(30)  // Longer for dev
    : TimeSpan.FromMinutes(1);   // Shorter for prod

builder.Services.AddSession(options =>
{
    options.IdleTimeout = sessionTimeout;
});
```

---

## Troubleshooting

### Issue: Session doesn't expire after 1 minute
**Check:**
1. Clear browser cookies
2. Restart application
3. Verify all three timeout settings match (1 minute)
4. Check if user is actively clicking (sliding expiration)
5. Review application logs for session activity

### Issue: Session expires too quickly
**Check:**
1. Ensure sliding expiration is enabled: `options.SlidingExpiration = true`
2. Verify user is making requests (timer resets on each request)
3. Check for JavaScript errors preventing requests
4. Review SessionValidationMiddleware logs

### Issue: Timeout message not showing
**Check:**
1. Verify URL has `?timeout=true` parameter
2. Check Login.cshtml.cs has `TimeoutMessage` property
3. Ensure `OnGet(bool timeout)` parameter is working
4. Clear browser cache

---

## Logs to Monitor

### Successful Timeout Flow
```
[Warning] Invalid or expired session for user {userId}
[Information] User logged out.
[Information] User logged in successfully: {email}
```

### Check Application Logs
```csharp
// Session expiration detected
_logger.LogWarning("Invalid or expired session for user {UserId}", user.Id);

// User signed out
_logger.LogInformation("User logged out.");

// User logs back in
_logger.LogInformation("User logged in successfully: {Email}", LModel.Email);
```

---

## User Communication

### Inform Users About Timeout Policy

**Via Login Page:**
- Session timeout notice displayed prominently
- Explains 1-minute inactivity limit
- Sets proper expectations

**Via Session Management Page:**
- Updated `/ActiveSessions` page
- Lists 1-minute timeout in security info
- Mentions auto-redirect behavior

**Via Email/Documentation (Recommended):**
```
Dear User,

For your security, our application now automatically logs you out 
after 1 minute of inactivity. 

This helps protect your account if you forget to logout or leave 
your device unattended.

When your session expires, you'll be redirected to the login page 
with a notification. Simply log back in to continue.

Thank you for your understanding.
```

---

## Verification Checklist

- [ ] ? Session timeout set to 1 minute in Program.cs (Session)
- [ ] ? Session timeout set to 1 minute in Program.cs (Auth Cookie)
- [ ] ? Session timeout set to 1 minute in SessionService.cs
- [ ] ? Sliding expiration enabled
- [ ] ? SessionValidationMiddleware registered in pipeline
- [ ] ? Timeout message displays on Login page
- [ ] ? Session timeout notice added to Login page
- [ ] ? ActiveSessions page updated with 1-minute info
- [ ] ? Build successful with no errors
- [ ] ? Middleware redirects to /Login?timeout=true
- [ ] ? Database sessions expire after 1 minute

---

## Summary

### What Changed
- ?? Session timeout: **30 minutes ? 1 minute**
- ?? Login page: Added timeout notification
- ?? Enhanced security with faster auto-logout
- ?? Clear user communication about timeout policy

### What Happens Now
1. User inactive for 1 minute ? Session expires
2. User tries to access page ? Redirected to login
3. Login page shows ? "Session Expired" warning
4. User logs back in ? New 1-minute session starts

### Build Status
? **Build Successful** - All changes compiled without errors

---

**Session timeout successfully configured to 1 minute with full user notification!** ?

Users will now be automatically logged out after 1 minute of inactivity and clearly notified when their session expires.
