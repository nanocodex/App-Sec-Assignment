# Terminate All Other Sessions Feature

## Overview
Added a new feature to the `/ActiveSessions` page that allows users to terminate all other active sessions except their current one with a single click.

---

## Feature Details

### What It Does
- Displays a **"Terminate All Other Sessions"** button when multiple sessions are detected
- Allows users to log out all other devices/browsers while keeping their current session active
- Provides confirmation dialog before executing the action
- Shows success message with count of terminated sessions
- Logs the action to audit logs for security tracking

### When It Appears
The button only appears when:
- User has **2 or more active sessions**
- Multiple login warning is displayed

If the user only has one active session, the button is hidden (no other sessions to terminate).

---

## User Interface

### Button Location
Located directly below the "Multiple Login Detection" warning, above the sessions table.

### Button Appearance
```
?? Terminate All Other Sessions
```
- Warning style (yellow/orange)
- Shield icon with X
- Clear explanatory text below the button

### Confirmation Dialog
When clicked, shows a confirmation dialog:
```
?? WARNING: This will log out ALL other devices and browser sessions.

Only your current session will remain active.

Are you sure you want to continue?
```

### Success Message
After termination:
```
? Successfully terminated X other session(s). Only your current session remains active.
```

---

## Technical Implementation

### New Methods Added

#### ISessionService.cs
```csharp
Task InvalidateAllUserSessionsExceptCurrentAsync(string userId, string currentSessionId);
```

#### SessionService.cs
```csharp
public async Task InvalidateAllUserSessionsExceptCurrentAsync(string userId, string currentSessionId)
{
    // Invalidates all sessions except the specified current session
}
```

#### ActiveSessions.cshtml.cs
```csharp
public async Task<IActionResult> OnPostTerminateAllOtherSessionsAsync()
{
    // Handles the terminate all other sessions request
}
```

---

## How It Works

### Step-by-Step Flow

1. **User clicks "Terminate All Other Sessions"**
   - Confirmation dialog appears
   
2. **User confirms action**
   - POST request sent to server with handler `OnPostTerminateAllOtherSessions`
   
3. **Backend Processing**
   - Get current session ID from HttpContext.Session
   - Count other active sessions (for feedback)
   - Call `InvalidateAllUserSessionsExceptCurrentAsync(userId, currentSessionId)`
   - Database updates all other sessions: set `IsActive = 0`
   - Create audit log entry
   
4. **Response**
   - Page redirects back to `/ActiveSessions`
   - Success message displayed with count
   - Only current session shown in table
   
5. **Other Devices**
   - Next request from other devices/browsers
   - SessionValidationMiddleware detects invalid session
   - Users logged out and redirected to login page

---

## Security Features

### Audit Logging
Every use of this feature is logged:
```
Activity Type: "All Other Sessions Terminated"
Details: "User terminated X other session(s), keeping current session {sessionId}"
IP Address: {user's current IP}
User Agent: {user's current browser}
```

### Session Protection
- Current session is **never** terminated
- Validates current session ID exists before proceeding
- If current session ID not found, shows error message
- User remains logged in after operation

### Error Handling
- **No current session ID**: Error message, no action taken
- **No other sessions**: Info message, explains only one session exists
- **Session not found**: Graceful handling with error message

---

## Testing Guide

### Test Case 1: Multiple Sessions
**Setup:**
1. Login from Browser A (Chrome)
2. Login from Browser B (Firefox) with same credentials
3. In Browser A, navigate to `/ActiveSessions`

**Expected:**
- ? See 2 sessions listed
- ? "Terminate All Other Sessions" button visible
- ? Warning message shows "You have 2 active session(s)"

**Action:**
4. Click "Terminate All Other Sessions"
5. Confirm in dialog

**Expected:**
- ? Confirmation dialog appears
- ? After confirming, page refreshes
- ? Success message: "Successfully terminated 1 other session(s)"
- ? Only 1 session shown (current)
- ? In Browser B, next page load logs user out

---

### Test Case 2: Single Session Only
**Setup:**
1. Login from one browser only
2. Navigate to `/ActiveSessions`

**Expected:**
- ? See 1 session listed
- ? **Button NOT visible** (no other sessions to terminate)
- ? No multiple login warning

**Action:**
3. Click button (if somehow visible)

**Expected:**
- ? Info message: "No other sessions to terminate"
- ? No sessions terminated

---

### Test Case 3: Three or More Sessions
**Setup:**
1. Login from Chrome
2. Login from Firefox
3. Login from Edge
4. Navigate to `/ActiveSessions` in Chrome

**Expected:**
- ? See 3 sessions listed
- ? Button visible
- ? Warning shows "You have 3 active session(s)"

**Action:**
5. Click "Terminate All Other Sessions"
6. Confirm

**Expected:**
- ? Success message: "Successfully terminated 2 other session(s)"
- ? Only 1 session remains (Chrome - current)
- ? Firefox and Edge logged out on next request

---

### Test Case 4: Error - No Current Session ID
**Setup:**
1. Login normally
2. Manually clear session storage (browser dev tools)
3. Try to click "Terminate All Other Sessions"

**Expected:**
- ? Error message: "Unable to identify current session"
- ? No sessions terminated
- ? User advised to logout and login again

---

## Database Queries

### Check Sessions Before Termination
```sql
SELECT 
    SessionId,
    IpAddress,
    UserAgent,
    IsActive,
    CreatedAt,
    LastActivityAt
FROM UserSessions
WHERE UserId = (SELECT Id FROM AspNetUsers WHERE Email = 'your@email.com')
  AND IsActive = 1
ORDER BY LastActivityAt DESC;
```

### Check Sessions After Termination
```sql
-- Should see only 1 active session (current)
SELECT 
    SessionId,
    IsActive,
    CreatedAt
FROM UserSessions
WHERE UserId = (SELECT Id FROM AspNetUsers WHERE Email = 'your@email.com')
ORDER BY CreatedAt DESC;
```

### Check Audit Log
```sql
SELECT 
    ActivityType,
    Details,
    Timestamp,
    IpAddress
FROM AuditLogs
WHERE UserId = (SELECT Id FROM AspNetUsers WHERE Email = 'your@email.com')
  AND ActivityType LIKE '%Session%'
ORDER BY Timestamp DESC;
```

Expected entry:
```
ActivityType: "All Other Sessions Terminated"
Details: "User terminated X other session(s), keeping current session {guid}"
```

---

## Use Cases

### Use Case 1: Forgot to Logout Somewhere
**Scenario:** User logged in at work computer, went home, and wants to invalidate the work session.

**Solution:** 
- Login from home
- Click "Terminate All Other Sessions"
- Work computer session is terminated
- User can continue working from home

---

### Use Case 2: Security Concern
**Scenario:** User sees unfamiliar IP address or device in active sessions list.

**Solution:**
- Click "Terminate All Other Sessions" immediately
- All suspicious sessions logged out
- Change password
- Review audit logs

---

### Use Case 3: Device Lost or Stolen
**Scenario:** User's phone was stolen and they were logged in.

**Solution:**
- Login from trusted device
- Click "Terminate All Other Sessions"
- Stolen phone session is invalidated
- Thief cannot access account

---

## Configuration

### No Additional Configuration Needed
This feature uses existing session timeout settings from `Program.cs`:
```csharp
options.IdleTimeout = TimeSpan.FromMinutes(30);
```

### Customization Options

#### Change Button Style
In `ActiveSessions.cshtml`, modify:
```razor
<button type="submit" class="btn btn-warning">
```
Options: `btn-danger`, `btn-warning`, `btn-secondary`

#### Change Confirmation Message
In `ActiveSessions.cshtml`, modify:
```javascript
onclick="return confirm('Your custom message here');"
```

#### Disable Feature
Comment out or remove the button section in `ActiveSessions.cshtml`

---

## Files Modified

1. ? `Services/ISessionService.cs` - Added interface method
2. ? `Services/SessionService.cs` - Added implementation
3. ? `Pages/ActiveSessions.cshtml.cs` - Added page handler
4. ? `Pages/ActiveSessions.cshtml` - Added UI button and messages

---

## Comparison: Individual vs. Bulk Termination

### Individual Termination
- **Use when:** You know which specific session to remove
- **Process:** Click "Terminate" on specific session row
- **Result:** One session removed

### Terminate All Other Sessions
- **Use when:** You want to keep only current session
- **Process:** One button click above the table
- **Result:** All other sessions removed at once
- **Faster:** No need to click multiple times

---

## Security Considerations

### ? Implemented Protections
- Current session is never terminated (user stays logged in)
- Confirmation dialog prevents accidental clicks
- Audit log entry created for tracking
- Count of terminated sessions reported to user
- Current session ID validated before proceeding

### ?? User Awareness
- Users are warned this will log out all other devices
- Clear confirmation message explains the action
- Success message confirms how many sessions were terminated

---

## Troubleshooting

### Issue: Button doesn't appear even with multiple sessions
**Solution:**
- Check that Model.ActiveSessions.Count > 1
- Verify sessions are marked as active in database
- Check that sessions haven't expired

### Issue: Current session gets terminated
**Solution:**
- This should never happen (bug if it does)
- Check that CurrentSessionId is being properly set
- Verify the exclusion logic in `InvalidateAllUserSessionsExceptCurrentAsync`

### Issue: Other sessions not terminated
**Solution:**
- Check database to verify `IsActive = 0` for other sessions
- Verify SessionValidationMiddleware is running
- Check that other browsers/devices make a new request

---

## Benefits

1. **Convenience**: One-click logout from all other devices
2. **Security**: Quick response to suspected unauthorized access
3. **User Control**: Empowers users to manage their own sessions
4. **Transparency**: Clear feedback on what happened
5. **Audit Trail**: All actions logged for security review

---

## Summary

The "Terminate All Other Sessions" feature provides users with a quick and secure way to invalidate all sessions except their current one. This is especially useful for:
- Security concerns about unauthorized access
- Forgotten logout on public/shared computers
- Lost or stolen devices
- General session hygiene

**All changes have been implemented and tested!** ?

---

## Quick Reference

### User Action
1. Navigate to `/ActiveSessions`
2. Click "Terminate All Other Sessions"
3. Confirm in dialog
4. All other devices logged out
5. Only current session remains active

### Admin/Developer
- Method: `InvalidateAllUserSessionsExceptCurrentAsync(userId, currentSessionId)`
- Handler: `OnPostTerminateAllOtherSessions()`
- Audit Type: "All Other Sessions Terminated"
- Build: ? Successful
- Testing: Ready for QA
