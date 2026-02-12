# Testing Guide - Credential Verification Features

## Quick Test Steps

### 1. Test User Registration
1. Navigate to `/Register`
2. Fill in all required fields:
   - Email
   - Password (must meet complexity requirements)
   - First Name, Last Name
   - Credit Card Number
   - Mobile, Billing, Shipping addresses
   - Upload a .jpg photo
3. Click "Register"
4. **Expected**: Automatically logged in and redirected to homepage
5. **Verify**: Homepage displays your profile with masked credit card

### 2. Test Login Success
1. Click "Logout" in the navbar
2. Navigate to `/Login`
3. Enter correct email and password
4. Click "Login"
5. **Expected**: Redirected to homepage with your profile displayed

### 3. Test Rate Limiting (Account Lockout)
1. Logout
2. Go to `/Login`
3. Enter **wrong password** - Attempt 1
4. **Expected**: "Invalid email or password" message
5. Enter **wrong password** - Attempt 2
6. **Expected**: "Invalid email or password" message
7. Enter **wrong password** - Attempt 3
8. **Expected**: Account locked message displayed
9. Try to login with **correct password**
10. **Expected**: Still locked out for 15 minutes

**To unlock for testing**: 
- Option 1: Wait 15 minutes
- Option 2: Clear lockout in database:
  ```sql
  UPDATE AspNetUsers 
  SET LockoutEnd = NULL, AccessFailedCount = 0 
  WHERE Email = 'your-email@example.com'
  ```

### 4. Test Logout
1. Login successfully
2. Click "Logout" in the navbar
3. **Expected**: Redirected to `/Login` page
4. Try to access `/Index` directly
5. **Expected**: Redirected back to login (session cleared)

### 5. Test Audit Logs
1. Login successfully
2. Navigate to homepage
3. Click "View Activity Log" button
4. **Expected**: See list of your activities including:
   - Registration
   - Login attempts (success/failed)
   - Logout
   - Profile views
5. Each entry should show:
   - Date/Time
   - Action (with color-coded badge)
   - Details
   - IP Address

### 6. Verify Database Audit Logs
Open SQL Server Management Studio or your database tool:

```sql
-- View all audit logs
SELECT * FROM AuditLogs ORDER BY Timestamp DESC;

-- View logs for specific user
SELECT * FROM AuditLogs 
WHERE UserId = 'USER_ID_HERE' 
ORDER BY Timestamp DESC;

-- Count activities by type
SELECT Action, COUNT(*) as Count 
FROM AuditLogs 
GROUP BY Action;
```

## Expected Audit Log Entries

After a complete test, you should see these entries:

1. **Registration** - When user registers
2. **Login Success** - When login succeeds
3. **View Profile** - When user views homepage
4. **Login Failed - Invalid Credentials** - For wrong password attempts
5. **Login Failed - Locked Out** - When attempting login while locked
6. **Logout** - When user logs out

## Security Verification Checklist

- [ ] Password must be at least 12 characters
- [ ] Password must contain uppercase, lowercase, digit, special character
- [ ] Account locks after 3 failed attempts
- [ ] Lockout lasts for 15 minutes
- [ ] User is redirected to homepage after successful login
- [ ] Credit card is encrypted in database
- [ ] Credit card is masked on homepage (shows last 4 digits)
- [ ] Session is cleared on logout
- [ ] Cannot access protected pages after logout
- [ ] All activities are logged in AuditLogs table
- [ ] IP address and User-Agent are captured in logs

## Common Issues & Solutions

### Issue: Can't login after registration
**Solution**: Check if email confirmation is required. In `Program.cs`, ensure:
```csharp
options.SignIn.RequireConfirmedEmail = false;
```

### Issue: Account locked but don't want to wait 15 minutes
**Solution**: Run this SQL to unlock:
```sql
UPDATE AspNetUsers 
SET LockoutEnd = NULL, AccessFailedCount = 0 
WHERE Email = 'your-email@example.com'
```

### Issue: Audit logs not appearing
**Solution**: 
1. Check if migration was applied: `dotnet ef database update`
2. Verify AuditLogs table exists in database
3. Check for errors in application logs

### Issue: Session not clearing on logout
**Solution**: Ensure session middleware is configured in `Program.cs`:
```csharp
app.UseSession(); // Must be before app.UseAuthentication()
```

## Performance Notes

- Audit logging is done asynchronously to not slow down user operations
- Failed audit log writes are logged but don't block user actions
- Database indexes on UserId and Timestamp recommended for large audit tables

## Next Steps

After verifying all features work:
1. Review audit logs to ensure all activities are captured
2. Test from different browsers/devices to see different User-Agent strings
3. Consider adding pagination to Audit Logs page for users with many entries
4. Consider admin dashboard to view all user activities (security monitoring)
