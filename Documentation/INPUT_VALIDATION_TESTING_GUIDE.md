# Input Validation Testing Guide

## Quick Test Scenarios

### 1. SQL Injection Prevention Tests

#### Test Case 1.1: SQL Injection in Email (Registration)
**Steps:**
1. Go to `/Register`
2. Enter email: `admin'--@example.com`
3. Fill other fields with valid data
4. Click Register

**Expected Result:**
- ? Client-side validation error: "Invalid characters detected"
- Form submission blocked

#### Test Case 1.2: SQL Injection in Email (Login)
**Steps:**
1. Go to `/Login`
2. Enter email: `' OR '1'='1`
3. Enter any password
4. Click Login

**Expected Result:**
- ? Server-side validation error: "Invalid email format"
- Login blocked

### 2. XSS Prevention Tests

#### Test Case 2.1: Script Tag in Name
**Steps:**
1. Go to `/Register`
2. Enter first name: `<script>alert('XSS')</script>`
3. Fill other fields
4. Click Register

**Expected Result:**
- ? Client-side validation error: "Name can only contain letters..."
- Form submission blocked

#### Test Case 2.2: JavaScript Protocol in Address
**Steps:**
1. Go to `/Register`
2. Enter billing address: `javascript:alert('XSS')`
3. Fill other fields
4. Click Register

**Expected Result:**
- ? Client-side validation error: "Address can only contain letters, numbers..."
- Form submission blocked

#### Test Case 2.3: HTML in Email
**Steps:**
1. Go to `/Register`
2. Enter email: `test<b>bold</b>@example.com`
3. Fill other fields
4. Click Register

**Expected Result:**
- ? Client-side validation error: "Invalid characters detected"
- Form submission blocked

### 3. Input Validation Tests

#### Test Case 3.1: Invalid Mobile Number Format
**Steps:**
1. Go to `/Register`
2. Enter mobile: `1234567` (7 digits, doesn't start with 8/9)
3. Click on another field (blur event)

**Expected Result:**
- ? Real-time validation error: "Mobile number must be 8 digits and start with 8 or 9"
- Red border around field

#### Test Case 3.2: Valid Mobile Number
**Steps:**
1. Go to `/Register`
2. Enter mobile: `81234567`
3. Click on another field (blur event)

**Expected Result:**
- ? Green border around field (validation success)

#### Test Case 3.3: Short Name
**Steps:**
1. Go to `/Register`
2. Enter first name: `A`
3. Click on another field

**Expected Result:**
- ? Validation error: "Name must be at least 2 characters long"
- Red border

#### Test Case 3.4: Name with Numbers
**Steps:**
1. Go to `/Register`
2. Enter first name: `John123`
3. Click on another field

**Expected Result:**
- ? Validation error: "Name can only contain letters, spaces, hyphens, and apostrophes"
- Red border

#### Test Case 3.5: Short Address
**Steps:**
1. Go to `/Register`
2. Enter billing address: `123`
3. Click on another field

**Expected Result:**
- ? Validation error: "Address is too short. Please enter a complete address"
- Red border

#### Test Case 3.6: Address with Special Characters
**Steps:**
1. Go to `/Register`
2. Enter billing address: `123 Main St @ Suite #5`
3. Click on another field

**Expected Result:**
- ? Validation error: "Address can only contain letters, numbers, spaces, and common punctuation (.,#-/)"
- Red border (@ symbol not allowed)

### 4. Credit Card Validation Tests

#### Test Case 4.1: Invalid Credit Card (Too Short)
**Steps:**
1. Go to `/Register`
2. Enter credit card: `1234 5678`
3. Click on another field

**Expected Result:**
- ? Validation error: "Credit card number must be between 13 and 19 digits"
- Red border

#### Test Case 4.2: Invalid Credit Card (Luhn Check Fails)
**Steps:**
1. Go to `/Register`
2. Enter credit card: `1234 5678 9012 3456` (invalid Luhn)
3. Click on another field

**Expected Result:**
- ? Validation error: "Please enter a valid credit card number"
- Red border

#### Test Case 4.3: Valid Credit Card
**Steps:**
1. Go to `/Register`
2. Enter credit card: `4111 1111 1111 1111` (valid Visa test card)
3. Click on another field

**Expected Result:**
- ? Green border (validation success)
- Number auto-formatted with spaces

**Note:** Use these valid test credit card numbers:
- **Visa**: `4111 1111 1111 1111` (recommended)
- **Visa**: `4012 8888 8888 1881`
- **Mastercard**: `5425 2334 3010 9903`
- **Amex**: `3782 822463 10005`
- **Discover**: `6011 1111 1111 1117`

### 5. Password Validation Tests

#### Test Case 5.1: Weak Password (Too Short)
**Steps:**
1. Go to `/Register`
2. Enter password: `Pass123!`
3. Observe real-time feedback

**Expected Result:**
- ? Password strength indicator shows "Weak"
- Red indicators for "At least 12 characters"

#### Test Case 5.2: Password Missing Uppercase
**Steps:**
1. Go to `/Register`
2. Enter password: `password123!`
3. Observe real-time feedback

**Expected Result:**
- ? Red indicator for "One uppercase letter"

#### Test Case 5.3: Strong Password
**Steps:**
1. Go to `/Register`
2. Enter password: `MyP@ssw0rd123!`
3. Observe real-time feedback

**Expected Result:**
- ? All requirements show green checkmarks
- Password strength indicator shows "Strong Password"

### 6. Email Validation Tests

#### Test Case 6.1: Invalid Email Format
**Steps:**
1. Go to `/Register`
2. Enter email: `notanemail`
3. Click on another field

**Expected Result:**
- ? Validation error: "Please enter a valid email address"
- Red border

#### Test Case 6.2: Email Too Long
**Steps:**
1. Go to `/Register`
2. Enter email: `verylongemailaddressthatexceedsonehundredcharacterslimitandshouldfailvalidation@example.com`
3. Click on another field

**Expected Result:**
- ? Validation error: "Email cannot exceed 100 characters"
- Red border

#### Test Case 6.3: Valid Email
**Steps:**
1. Go to `/Register`
2. Enter email: `test@example.com`
3. Click on another field

**Expected Result:**
- ? Green border (validation success)

### 7. File Upload Validation Tests

#### Test Case 7.1: Wrong File Type
**Steps:**
1. Go to `/Register`
2. Select a PNG or PDF file for photo
3. Fill other fields and submit

**Expected Result:**
- ? Server-side error: "Only .jpg and .jpeg files are allowed"

#### Test Case 7.2: File Too Large
**Steps:**
1. Go to `/Register`
2. Select a JPG file larger than 5MB
3. Fill other fields and submit

**Expected Result:**
- ? Server-side error: "Photo size cannot exceed 5MB"

#### Test Case 7.3: Valid Photo Upload
**Steps:**
1. Go to `/Register`
2. Select a JPG file under 5MB
3. Fill all other fields correctly
4. Submit form

**Expected Result:**
- ? Registration successful
- Photo uploaded to `/wwwroot/uploads/photos/`

### 8. CSRF Protection Tests

#### Test Case 8.1: Form Submission Without Token
**Steps:**
1. Open browser developer tools
2. Go to `/Register`
3. In console, remove anti-forgery token: `document.querySelector('input[name="__RequestVerificationToken"]').remove()`
4. Fill form and submit

**Expected Result:**
- ? HTTP 400 Bad Request
- Error: "The required antiforgery token was not supplied"

### 9. reCAPTCHA Validation Tests

#### Test Case 9.1: Registration Without reCAPTCHA
**Steps:**
1. Disable JavaScript in browser
2. Go to `/Register`
3. Fill all fields correctly
4. Submit form

**Expected Result:**
- ? Server-side error: "reCAPTCHA validation failed"

#### Test Case 9.2: Login Without reCAPTCHA
**Steps:**
1. Disable JavaScript in browser
2. Go to `/Login`
3. Enter credentials
4. Submit form

**Expected Result:**
- ? Server-side error: "reCAPTCHA validation failed"

### 10. Combined Validation Test (Happy Path)

#### Test Case 10.1: Complete Valid Registration
**Steps:**
1. Go to `/Register`
2. Enter data:
   - First Name: `John`
   - Last Name: `Doe`
   - Email: `john.doe@example.com`
   - Mobile: `81234567`
   - Credit Card: `4111111111111111` (or `4111 1111 1111 1111` with spaces)
   - Billing: `123 Main Street, #01-234, Singapore 123456`
   - Shipping: `456 Orchard Road, #02-345, Singapore 654321`
   - Password: `MySecureP@ssw0rd123`
   - Confirm Password: `MySecureP@ssw0rd123`
   - Photo: Select valid JPG file
3. Submit form

**Expected Result:**
- ? All client-side validations pass (green borders)
- ? reCAPTCHA verification successful
- ? All server-side validations pass
- ? User created successfully
- ? Redirected to homepage
- ? User data encrypted and stored in database

## Automated Testing (Optional)

You can also test validation programmatically:

### Using Browser Console

```javascript
// Test email validation
InputValidation.validateEmail('test@example.com');
// Returns: { valid: true, message: '' }

InputValidation.validateEmail('invalid-email');
// Returns: { valid: false, message: 'Please enter a valid email address' }

// Test mobile validation
InputValidation.validateMobile('81234567');
// Returns: { valid: true, message: '' }

InputValidation.validateMobile('1234567');
// Returns: { valid: false, message: 'Mobile number must be 8 digits...' }

// Test XSS detection
InputValidation.containsXss('<script>alert("XSS")</script>');
// Returns: true

InputValidation.containsXss('Normal text');
// Returns: false

// Test SQL injection detection
InputValidation.containsSqlInjection("' OR '1'='1");
// Returns: true

InputValidation.containsSqlInjection('normal@example.com');
// Returns: false
```

## Server-Side Testing (Using API)

You can test server-side validation by submitting forms with invalid data:

```bash
# Test SQL injection in registration
curl -X POST https://localhost:7XXX/Register \
  -d "RModel.Email=admin'--@example.com" \
  -d "RModel.Password=Test123!@#" \
  # ... other fields
```

## Security Headers Verification

Check security headers using browser developer tools:

1. Open Network tab
2. Navigate to any page
3. Click on the request
4. Check Response Headers:
   - `Content-Security-Policy`
   - `X-Content-Type-Options: nosniff`
   - `X-Frame-Options: SAMEORIGIN`
   - `X-XSS-Protection: 1; mode=block`

## Testing Checklist

- [ ] SQL Injection blocked in all input fields
- [ ] XSS attacks blocked (script tags, JavaScript protocol, etc.)
- [ ] CSRF protection working (anti-forgery token required)
- [ ] Email format validation (client and server)
- [ ] Mobile number format validation (8 digits, starts with 8/9)
- [ ] Name validation (letters, spaces, hyphens, apostrophes only)
- [ ] Address validation (5-200 chars, safe characters only)
- [ ] Credit card validation (Luhn check, 13-19 digits)
- [ ] Password strength validation (12+ chars, complexity)
- [ ] File upload validation (JPG only, max 5MB)
- [ ] Real-time client-side validation feedback
- [ ] Server-side validation for all inputs
- [ ] Proper error messages displayed
- [ ] Input sanitization working
- [ ] Security headers present
- [ ] reCAPTCHA verification working

## Common Issues and Solutions

### Issue: Client-side validation not working
**Solution:** 
- Check browser console for JavaScript errors
- Ensure `input-validation.js` is loaded
- Verify input field IDs match JavaScript selectors

### Issue: Server-side validation always passes
**Solution:**
- Check ModelState.IsValid in controller
- Verify attributes are applied to ViewModel properties
- Check service registration in Program.cs

### Issue: reCAPTCHA verification fails
**Solution:**
- Check reCAPTCHA site key in appsettings.json
- Verify domain is allowed in Google reCAPTCHA console
- Check network connectivity to Google servers

### Issue: Form submission blocked by CSRF
**Solution:**
- Ensure anti-forgery token is included in form
- Check cookie settings (SameSite, Secure)
- Verify HTTPS is enabled

## Conclusion

This testing guide covers all aspects of input validation and security:
- ? Injection attack prevention (SQL, XSS)
- ? CSRF protection
- ? Input validation (format, length, patterns)
- ? File upload security
- ? Password strength
- ? Real-time feedback
- ? Security headers

Test each scenario to ensure your application is secure and user-friendly!
