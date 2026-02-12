# Session Management - Testing Checklist

## Quick Test Guide

### ? Pre-Testing Setup
- [x] Database migration applied (`dotnet ef database update`)
- [x] Build successful
- [x] Application running (F5)

---

## Test 1: Secured Session Creation

**Steps:**
1. Navigate to `/Login`
2. Login with valid credentials
3. Navigate to `/ActiveSessions`

**Expected Results:**
- ? Login successful
- ? Redirected to home page
- ? One active session displayed
- ? Session shows current IP and browser
- ? Session marked as "Current Session" (green badge)

**Database Check:**
```sql
SELECT * FROM UserSessions WHERE IsActive = 1 ORDER BY CreatedAt DESC;
```
Expected: 1 row with your session details

---

## Test 2: Session Timeout & Redirect

**Option A: Quick Test (Modify timeout temporarily)**

1. Stop the application
2. In `Program.cs`, change session timeout to 1 minute:
   ```csharp
   options.IdleTimeout = TimeSpan.FromMinutes(1);
   ```
3. In `SessionService.cs`, change timeout to 1 minute:
   ```csharp
   private readonly TimeSpan _sessionTimeout = TimeSpan.FromMinutes(1);
   ```
4. Restart application (F5)
5. Login
6. Wait 65 seconds
7. Try to navigate to any page (e.g., `/Privacy`)

**Expected Results:**
- ? After 1 minute, redirected to `/Login?timeout=true`
- ? Message displayed: "Your session has expired. Please log in again."
- ? Session cleared from browser
- ? Database session marked as inactive

**Option B: Normal Test (30 minutes)**
- Wait 30 minutes after login
- Try to access any protected page
- Should timeout and redirect

**Restore after testing:**
```csharp
// Program.cs
options.IdleTimeout = TimeSpan.FromMinutes(30);

// SessionService.cs
private readonly TimeSpan _sessionTimeout = TimeSpan.FromMinutes(30);
```

---

## Test 3: Multiple Device Detection

**Steps:**
1. Login using Chrome browser
2. Note you're logged in
3. Open Firefox browser (or Chrome Incognito)
4. Navigate to same site
5. Login with SAME credentials
6. In either browser, navigate to `/ActiveSessions`

**Expected Results:**
- ? Login successful on both browsers
- ? `/ActiveSessions` shows **2 active sessions**
- ? Current session highlighted in green
- ? Other session marked with warning badge "Other Device"
- ? Different IP/Browser info shown for each

**Check Application Logs:**
```
User logged in successfully: {email}
Multiple login detected for user {userId}. Active sessions: {count}
```

**Check Audit Logs:**
- Navigate to `/AuditLogs` (if available)
- Should see "Multiple Login Detected" entry

---

## Test 4: Session Termination

**Prerequisites:** Must have multiple active sessions (see Test 3)

**Steps:**
1. Navigate to `/ActiveSessions`
2. Verify you have 2+ sessions
3. Click "Terminate" button on a non-current session
4. Confirm the termination

**Expected Results:**
- ? Session removed from active sessions list
- ? If you try to use that browser/tab, you'll be logged out
- ? Database session marked `IsActive = 0`

**Test the terminated session:**
1. Go to the browser where you terminated the session
2. Try to access any page
3. Should be redirected to login (session invalidated)

---

## Test 5: Session Activity Update

**Steps:**
1. Login to the application
2. Note the "Last Activity" time in `/ActiveSessions`
3. Navigate to different pages (Home, Privacy, etc.)
4. Refresh `/ActiveSessions` page

**Expected Results:**
- ? "Last Activity" timestamp updates with each page visit
- ? "Expires At" timestamp extends (sliding expiration)
- ? Session remains active

---

## Test 6: Automatic Session Cleanup

**Steps:**
1. Login to create a session
2. In SQL Server Management Studio, manually expire a session:
   ```sql
   UPDATE UserSessions 
   SET ExpiresAt = DATEADD(MINUTE, -5, GETUTCDATE())
   WHERE Id = {session_id};
   ```
3. Wait up to 10 minutes
4. Check database again

**Expected Results:**
- ? Background service marks expired session as inactive
- ? Application logs show: "Cleaned up {count} expired sessions"
- ? Database: `IsActive = 0` for expired session

**Manual trigger (alternative):**
The cleanup service runs automatically every 10 minutes. To test immediately, you would need to restart the application.

---

## Test 7: Session Security Features

**Test Cookie Security:**
1. Login to application
2. Press F12 (Developer Tools)
3. Go to "Application" or "Storage" tab
4. Check cookies

**Expected Cookie Properties:**
- ? `HttpOnly: true` (not accessible via JavaScript)
- ? `Secure: true` (only transmitted over HTTPS)
- ? `SameSite: Strict` (CSRF protection)

**Test Session Validation:**
1. Login to application
2. In browser console, try to access session:
   ```javascript
   document.cookie
   ```
3. Should NOT see session data (HttpOnly protection)

---

## Test 8: Logout Session Cleanup

**Steps:**
1. Login to application
2. Navigate to `/ActiveSessions`
3. Note your session ID
4. Click "Logout" in navigation
5. Check database

**Expected Results:**
- ? Redirected to `/Login`
- ? Database session marked `IsActive = 0`
- ? Cannot access protected pages without re-login

**Database Check:**
```sql
SELECT * FROM UserSessions 
WHERE SessionId = '{your_session_id}';
```
Expected: `IsActive = 0`

---

## Test 9: Concurrent Session Management

**Steps:**
1. Login from Device A (Chrome)
2. Login from Device B (Firefox)
3. Login from Device C (Edge)
4. Navigate to `/ActiveSessions`

**Expected Results:**
- ? All 3 sessions shown
- ? Current session highlighted
- ? Can terminate other sessions individually
- ? All sessions tracked in database

**Optional: Test Single Session Mode**
1. Uncomment in `Login.cshtml.cs`:
   ```csharp
   await _sessionService.InvalidateAllUserSessionsAsync(user.Id);
   ```
2. Repeat test above
3. Expected: Only newest session remains active

---

## Database Verification Queries

### Check Active Sessions
```sql
SELECT 
    u.Email,
    s.SessionId,
    s.IpAddress,
    s.UserAgent,
    s.CreatedAt,
    s.LastActivityAt,
    s.ExpiresAt,
    s.IsActive,
    CASE 
        WHEN s.ExpiresAt > GETUTCDATE() AND s.IsActive = 1 THEN 'ACTIVE'
        WHEN s.ExpiresAt <= GETUTCDATE() THEN 'EXPIRED'
        ELSE 'TERMINATED'
    END AS Status
FROM UserSessions s
INNER JOIN AspNetUsers u ON s.UserId = u.Id
ORDER BY s.CreatedAt DESC;
```

### Check Sessions for Specific User
```sql
SELECT * FROM UserSessions 
WHERE UserId = (SELECT Id FROM AspNetUsers WHERE Email = 'your@email.com')
ORDER BY CreatedAt DESC;
```

### Clean Up All Test Sessions
```sql
-- Mark all sessions as inactive (for testing)
UPDATE UserSessions SET IsActive = 0;

-- Delete all sessions (for clean slate)
DELETE FROM UserSessions;
```

---

## Application Logs to Monitor

Watch for these log messages during testing:

### Login:
```
[Information] User logged in successfully: {email}
[Warning] Multiple login detected for user {userId}. Active sessions: {count}
[Information] Session created for user {userId} from {ipAddress}
```

### Session Validation:
```
[Warning] No session ID found for authenticated user {userId}
[Warning] Invalid or expired session for user {userId}
```

### Logout:
```
[Information] Session invalidated for user {userId}
[Information] User logged out.
```

### Cleanup:
```
[Information] Session Cleanup Service started
[Information] Cleaned up {count} expired sessions
[Information] Session cleanup completed at {time}
```

---

## Common Issues & Solutions

### Issue: Session not created after login
**Solution:**
- Check database connection
- Verify migration applied
- Check application logs for errors

### Issue: Always redirected to login
**Solution:**
- Check session timeout settings
- Verify middleware order in `Program.cs`
- Check browser cookies enabled

### Issue: Multiple logins not detected
**Solution:**
- Verify sessions are being created in database
- Check `GetActiveSessionCountAsync()` is called
- Review application logs

### Issue: Background cleanup not working
**Solution:**
- Verify `SessionCleanupService` registered in `Program.cs`
- Application must be running (not just debugging)
- Wait at least 10 minutes

---

## Performance Testing

### Load Test: Multiple Sessions
1. Create 10+ users
2. Login each user from 2 different browsers
3. Navigate to `/ActiveSessions` repeatedly
4. Monitor database query performance

**Expected:**
- ? Page loads in < 500ms
- ? Database queries use indexes
- ? No memory leaks

### Cleanup Performance
1. Create 100+ sessions in database (test data)
2. Set all to expired
3. Wait for cleanup service
4. Monitor application logs

**Expected:**
- ? Cleanup completes in < 10 seconds
- ? No application slowdown

---

## Security Testing

### Test 1: Session Hijacking Prevention
1. Login and get session cookie
2. Try to modify session ID in cookie
3. Try to access protected page

**Expected:** Redirected to login (invalid session)

### Test 2: Session Fixation Prevention
1. Get session ID before login
2. Login
3. Check if session ID changed

**Expected:** New session ID generated after login

### Test 3: CSRF Protection
1. Login to application
2. Create external form posting to logout endpoint
3. Try to submit from different site

**Expected:** Request blocked (SameSite=Strict)

---

## Checklist Summary

- [ ] Test 1: Secured session creation ?
- [ ] Test 2: Session timeout & redirect ?
- [ ] Test 3: Multiple device detection ?
- [ ] Test 4: Session termination ?
- [ ] Test 5: Activity update ?
- [ ] Test 6: Automatic cleanup ?
- [ ] Test 7: Security features ?
- [ ] Test 8: Logout cleanup ?
- [ ] Test 9: Concurrent sessions ?
- [ ] Database verification ?
- [ ] Log monitoring ?

---

## Production Deployment Checklist

Before deploying to production:

- [ ] Restore session timeout to 30 minutes (if changed for testing)
- [ ] Review single vs. multiple session policy
- [ ] Configure appropriate cleanup interval
- [ ] Set up monitoring/alerts for session-related errors
- [ ] Test on staging environment
- [ ] Review security settings
- [ ] Document session policies for users
- [ ] Train support staff on session management features

---

**Happy Testing!** ??

All session management features are fully implemented and ready for testing.
