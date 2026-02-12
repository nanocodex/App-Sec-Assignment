# Web Application Security Checklist

## Registration and User Data Management
- [x] Implement successful saving of member info into the database
- [ ] Check for duplicate email addresses and handle appropriately
- [x] Implement strong password requirements:
  - [x] Minimum 12 characters
  - [x] Combination of lowercase, uppercase, numbers, and special characters
  - [x] Provide feedback on password strength
  - [x] Implement both client-side and server-side password checks
- [x] Encrypt sensitive user data in the database (e.g., NRIC, credit card numbers)
- [x] Implement proper password hashing and storage
- [x] Implement file upload restrictions (e.g., .docx, .pdf, or .jpg only)

## Session Management
- [ ] Create a secure session upon successful login
- [ ] Implement session timeout
- [ ] Route to homepage/login page after session timeout
- [ ] Detect and handle multiple logins from different devices/browser tabs

## Login/Logout Security
- [ ] Implement proper login functionality
- [ ] Implement rate limiting (e.g., account lockout after 3 failed login attempts)
- [ ] Perform proper and safe logout (clear session and redirect to login page)
- [ ] Implement audit logging (save user activities in the database)
- [ ] Redirect to homepage after successful login, displaying user info

## Anti-Bot Protection
- [ ] Implement Google reCAPTCHA v3 service

## Input Validation and Sanitization
- [x] Prevent injection attacks (e.g., SQL injection) - *Using parameterized queries via Entity Framework*
- [x] Implement Cross-Site Request Forgery (CSRF) protection - *Built-in to Razor Pages*
- [ ] Prevent Cross-Site Scripting (XSS) attacks
- [x] Perform proper input sanitization, validation, and verification for all user inputs
- [x] Implement both client-side and server-side input validation
- [x] Display error or warning messages for improper input
- [ ] Perform proper encoding before saving data into the database

## Error Handling
- [ ] Implement graceful error handling on all pages
- [ ] Create and display custom error pages (e.g., 404, 403)

## Software Testing and Security Analysis
- [ ] Perform source code analysis using external tools (e.g., GitHub)
- [ ] Address security vulnerabilities identified in the source code

## Advanced Security Features
- [ ] Implement automatic account recovery after lockout period
- [ ] Enforce password history (avoid password reuse, max 2 password history)
- [ ] Implement change password functionality
- [ ] Implement reset password functionality (using email link or SMS)
- [ ] Enforce minimum and maximum password age policies
- [ ] Implement Two-Factor Authentication (2FA)

## General Security Best Practices
- [x] Use HTTPS for all communications - *ASP.NET Core default*
- [x] Implement proper access controls and authorization - *Using [Authorize] attribute*
- [ ] Keep all software and dependencies up to date
- [x] Follow secure coding practices
- [ ] Regularly backup and securely store user data
- [ ] Implement logging and monitoring for security events

## Documentation and Reporting
- [x] Prepare a report on implemented security features - *See SECURITY_IMPLEMENTATION_GUIDE.md*
- [ ] Complete and submit the security checklist

## ? Recently Completed (Current Session)
- **Strong Password Requirements**: Implemented comprehensive password validation with:
  - Custom `StrongPasswordAttribute` for model-level validation
  - Custom `CustomPasswordValidator` for Identity framework integration
  - Client-side real-time password strength indicator with visual feedback
  - Server-side triple-layer validation (attribute, Identity, built-in options)
  
- **Data Encryption**: Implemented centralized encryption service:
  - `EncryptionService` using ASP.NET Core Data Protection API
  - Credit card encryption in database
  - Secure decryption with masked display (last 4 digits only)
  
- **Homepage Display**: Updated Index page to:
  - Require authentication (`[Authorize]` attribute)
  - Display decrypted user information securely
  - Show profile with encrypted data properly decrypted

- **Code Quality**: Fixed all nullability warnings in modified files

## ?? Recommended Next Steps (Priority Order)

### High Priority (Security Critical)
1. **Login/Logout Security**
   - Create Login.cshtml and Login.cshtml.cs pages
   - Implement proper logout functionality
   - Add account lockout after failed attempts
   - Add audit logging for user activities

2. **Session Management**
   - Configure session timeout in Program.cs
   - Add session validation middleware
   - Handle concurrent login detection

3. **XSS Protection**
   - Review all user input display points
   - Ensure proper HTML encoding
   - Implement Content Security Policy (CSP) headers

### Medium Priority (UX + Security)
4. **Duplicate Email Check**
   - Add validation in Register page to check existing emails
   - Provide user-friendly error messages

5. **Error Handling**
   - Create custom error pages (404, 500, 403)
   - Implement global exception handling
   - Add graceful error recovery

6. **reCAPTCHA v3**
   - Register for Google reCAPTCHA
   - Integrate on registration and login pages
   - Configure score threshold

### Low Priority (Advanced Features)
7. **Password Management**
   - Change password page
   - Reset password with email verification
   - Password history tracking
   - Password age policies

8. **Two-Factor Authentication**
   - Implement 2FA setup page
   - Add authenticator app support
   - SMS backup codes

9. **Security Analysis**
   - Run static code analysis tools
   - Fix identified vulnerabilities
   - Document security audit results

Remember to test each security feature thoroughly and ensure they work as expected in your web application.
