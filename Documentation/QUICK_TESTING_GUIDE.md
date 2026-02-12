# Quick Testing Guide - Securing User Data and Passwords

## ? Quick Verification Checklist

### 1. Password Protection Testing (5 minutes)

#### Test Strong Password Requirements

**Scenario A: Password Too Short**
```
1. Navigate to: /Register
2. Fill in email: test1@example.com
3. Enter password: Pass123!
4. Expected: ? Error message - "Password must be at least 12 characters long."
```

**Scenario B: Missing Uppercase**
```
1. Enter password: password123!
2. Expected: ? Error - "Password must contain at least one uppercase letter (A-Z)."
```

**Scenario C: Missing Lowercase**
```
1. Enter password: PASSWORD123!
2. Expected: ? Error - "Password must contain at least one lowercase letter (a-z)."
```

**Scenario D: Missing Digit**
```
1. Enter password: Password!@#$
2. Expected: ? Error - "Password must contain at least one number (0-9)."
```

**Scenario E: Missing Special Character**
```
1. Enter password: Password1234
2. Expected: ? Error - "Password must contain at least one special character."
```

**Scenario F: Valid Strong Password**
```
1. Enter password: MySecure@Pass123
2. Fill in all other required fields:
   - First Name: John
   - Last Name: Doe
   - Credit Card: 4532015112830366
   - Mobile: +1234567890
   - Billing: 123 Main St, City, State
   - Shipping: 123 Main St, City, State
   - Photo: Upload any .jpg file
3. Click "Register"
4. Expected: ? Registration successful, automatically logged in, redirected to homepage
```

---

### 2. Encryption Testing (3 minutes)

#### Verify Credit Card is Encrypted in Database

**Step 1: Register a User**
```
1. Complete registration with credit card: 4532015112830366
2. Remember this number for verification
```

**Step 2: Check Database**
```sql
-- Open SQL Server Management Studio or your database tool
-- Run this query:

SELECT 
    Email,
    CreditCard,
    LEN(CreditCard) as EncryptedLength
FROM AspNetUsers 
WHERE Email = 'test1@example.com'
```

**Expected Results:**
```
Email: test1@example.com
CreditCard: CfDJ8Nq... (long encrypted string, ~200+ characters)
EncryptedLength: 200+ characters

? PASS: Credit card is encrypted (NOT plain text)
? FAIL: If you see "4532015112830366" - encryption is NOT working
```

**What Encrypted Data Looks Like:**
```
Good (Encrypted): CfDJ8Nq5ZBXqL9wK8F3mPqJZ0A1b2C3d4E5f6G7h8I9j0K1L2M3N4O5P6Q7R8S9T0...
Bad (Plain Text):  4532015112830366
```

---

### 3. Decryption and Display Testing (2 minutes)

#### Verify Homepage Shows Masked Credit Card

**Step 1: Login**
```
1. Navigate to: /Login
2. Enter email: test1@example.com
3. Enter password: MySecure@Pass123
4. Click "Login"
5. Expected: ? Redirected to homepage
```

**Step 2: Verify Profile Display**
```
On the homepage, you should see:

? Full Name: John Doe
? Email: test1@example.com
? Mobile: +1234567890
? Billing Address: 123 Main St, City, State
? Shipping Address: 123 Main St, City, State
? Credit Card: ****-****-****-0366 (last 4 digits only)
   (Last 4 digits shown for security)
? Photo: [Your uploaded photo]
```

**Expected Behavior:**
```
? PASS: Credit card shows as ****-****-****-0366
? PASS: Only last 4 digits visible
? FAIL: If you see full number 4532015112830366
? FAIL: If you see encrypted string CfDJ8...
```

---

### 4. Authorization Testing (2 minutes)

#### Verify Only Authenticated Users Can View Decrypted Data

**Step 1: Logout**
```
1. Click "Logout" in navigation bar
2. Expected: ? Redirected to /Login
```

**Step 2: Try Direct Access**
```
1. In browser address bar, type: http://localhost:5000/Index
2. Press Enter
3. Expected: ? Redirected back to /Login (access denied)
```

**Step 3: Login and Verify Access**
```
1. Login with valid credentials
2. Expected: ? Can view homepage with decrypted (but masked) data
```

---

## Database Verification Commands

### Check Encrypted Credit Cards
```sql
-- View all users and their encrypted credit cards
SELECT 
    Id,
    Email,
    UserName,
    LEFT(CreditCard, 50) + '...' as EncryptedCreditCard,
    LEN(CreditCard) as Length
FROM AspNetUsers
ORDER BY Email;
```

### Check Password Hashes
```sql
-- View password hashes (should be long hashed strings)
SELECT 
    Email,
    LEFT(PasswordHash, 50) + '...' as HashedPassword,
    LEN(PasswordHash) as HashLength
FROM AspNetUsers
WHERE Email = 'test1@example.com';
```

**Expected:**
- PasswordHash should be ~100+ characters
- Should look like: `AQAAAAIAAYag...`
- Should NOT be plain text password

### View Audit Logs
```sql
-- Check audit trail for registration and login
SELECT 
    Action,
    Timestamp,
    Details,
    IpAddress
FROM AuditLogs
WHERE UserId = (SELECT Id FROM AspNetUsers WHERE Email = 'test1@example.com')
ORDER BY Timestamp DESC;
```

**Expected Entries:**
- ? Registration
- ? Login Success
- ? View Profile

---

## Common Issues and Solutions

### Issue 1: "Password is too short" even with 12+ characters
**Cause:** Spaces or special characters might be counted incorrectly
**Solution:** Make sure password is exactly what you think it is (no extra spaces)

### Issue 2: Credit card shows as `****` on homepage
**Cause:** Decryption failed (likely encryption keys changed)
**Solution:** 
1. Delete user from database
2. Re-register
3. If issue persists, check `Program.cs` has `builder.Services.AddDataProtection()`

### Issue 3: Can access homepage without login
**Cause:** `[Authorize]` attribute might be missing
**Solution:** Verify `Pages/Index.cshtml.cs` has `[Authorize]` attribute on the class

### Issue 4: Credit card shows full number on homepage
**Cause:** Masking logic not working
**Solution:** Check `Pages/Index.cshtml` has the substring logic for last 4 digits

### Issue 5: See encrypted string on homepage instead of masked number
**Cause:** Decryption not happening
**Solution:** Verify `Index.cshtml.cs` calls `_encryptionService.Decrypt()`

---

## Expected vs Actual Results

| Test | What You Should See | What You Should NOT See |
|------|---------------------|-------------------------|
| **Database - Credit Card** | `CfDJ8Nq5ZBX...` (encrypted) | `4532015112830366` (plain text) |
| **Database - Password** | `AQAAAAIAAYag...` (hashed) | `MySecure@Pass123` (plain text) |
| **Homepage - Credit Card** | `****-****-****-0366` (masked) | `4532015112830366` (full number) |
| **Homepage - Credit Card** | `****-****-****-0366` (masked) | `CfDJ8Nq5ZBX...` (encrypted string) |
| **Logout - Access** | Redirected to `/Login` | Can still view homepage |

---

## Security Checklist

After testing, verify these security measures:

- [ ] Passwords require 12+ characters
- [ ] Passwords require uppercase, lowercase, digit, special character
- [ ] Password is hashed in database (not plain text)
- [ ] Credit card is encrypted in database (not plain text)
- [ ] Credit card is decrypted on homepage
- [ ] Credit card is masked (shows only last 4 digits)
- [ ] Homepage requires authentication
- [ ] Logout clears session and redirects to login
- [ ] Cannot access homepage after logout
- [ ] Audit logs track registration, login, profile views

---

## Complete Test Flow (10 minutes end-to-end)

```
1. Register new user with strong password ?
   ??> Check database: password hashed, credit card encrypted ?

2. Verify auto-login and redirect to homepage ?
   ??> Check homepage: credit card masked (last 4 digits only) ?

3. Logout ?
   ??> Try accessing homepage: redirected to login ?

4. Login again ?
   ??> Homepage accessible, data still masked ?

5. Check audit logs ?
   ??> Registration, login, logout, profile views logged ?
```

---

## Success Criteria

? **All three requirements met:**

1. **Password Protection**: Strong passwords enforced, hashed in database
2. **Encryption**: Credit card encrypted in database using Data Protection API
3. **Decryption**: Credit card decrypted on homepage and displayed masked

Your implementation is **PRODUCTION READY** for password and data security! ??
