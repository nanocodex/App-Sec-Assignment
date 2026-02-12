# Input Validation and Security Implementation Guide

## Overview
This document describes the comprehensive input validation, sanitization, and security measures implemented to prevent injection attacks (SQL Injection, XSS, CSRF) and ensure data integrity.

## Security Features Implemented

### 1. SQL Injection Prevention ?

**Primary Defense: Entity Framework Core**
- All database queries use parameterized queries through Entity Framework
- No raw SQL queries or string concatenation
- Automatic parameter escaping

**Secondary Defense: Input Validation**
- `NoSqlInjectionAttribute` validates input patterns
- `InputSanitizationService.ContainsPotentialSqlInjection()` detects SQL keywords
- Server-side validation in Register and Login pages

**Implementation:**
```csharp
// Attributes/NoSqlInjectionAttribute.cs
[NoSqlInjection]
public string Email { get; set; }

// Services/InputSanitizationService.cs
if (_sanitizationService.ContainsPotentialSqlInjection(input))
{
    // Reject input
}
```

### 2. Cross-Site Scripting (XSS) Prevention ?

**Multiple Layers of Protection:**

**A. HTML Encoding**
- Razor Pages automatically HTML-encode output: `@Model.UserInput`
- Manual encoding via `HtmlEncoder` for special cases
- Content Security Policy (CSP) headers restrict script sources

**B. Input Validation**
- `NoHtmlAttribute` prevents HTML tags in input
- `InputSanitizationService.ContainsPotentialXss()` detects XSS patterns
- Client-side validation blocks suspicious patterns

**C. Security Headers**
```
Content-Security-Policy: Restricts script sources
X-XSS-Protection: 1; mode=block
X-Content-Type-Options: nosniff
```

**Implementation:**
```csharp
// ViewModels
[NoHtml]
public string Email { get; set; }

// Services
public string SanitizeHtml(string html)
{
    return _htmlEncoder.Encode(html);
}
```

### 3. Cross-Site Request Forgery (CSRF) Protection ?

**Built-in Razor Pages Protection:**
- Anti-forgery tokens automatically included in forms
- Token validation on POST requests
- Configured with secure cookie settings

**Implementation:**
```csharp
// Program.cs
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
});
```

**In Razor Pages:**
```html
<form method="post">
    <!-- Anti-forgery token automatically included -->
</form>
```

### 4. Input Sanitization ?

**Server-Side Sanitization Service**

**Features:**
- Remove null characters and control characters
- Normalize line endings
- Trim whitespace
- Strip HTML tags
- Detect XSS and SQL injection patterns

**Implementation:**
```csharp
// Services/IInputSanitizationService.cs
public interface IInputSanitizationService
{
    string SanitizeInput(string input);
    string SanitizeHtml(string html);
    string StripHtml(string html);
    bool ContainsPotentialXss(string input);
    bool ContainsPotentialSqlInjection(string input);
}
```

**Usage:**
```csharp
var sanitizedEmail = _sanitizationService.SanitizeInput(RModel.Email);
```

### 5. Input Validation (Client & Server) ?

**A. Server-Side Validation Attributes**

| Attribute | Purpose | Usage |
|-----------|---------|-------|
| `NameValidationAttribute` | Validates names (letters, spaces, hyphens, apostrophes only) | First Name, Last Name |
| `SingaporeMobileAttribute` | Validates Singapore mobile format (8 digits, starts with 8/9) | Mobile Number |
| `AddressValidationAttribute` | Validates address format (5-200 chars, safe characters) | Billing/Shipping Address |
| `NoHtmlAttribute` | Prevents HTML/script tags | Email, all text inputs |
| `NoSqlInjectionAttribute` | Detects SQL injection patterns | All text inputs |
| `StrongPasswordAttribute` | Enforces strong password requirements | Password |

**Example:**
```csharp
[Required(ErrorMessage = "First name is required")]
[NameValidation]
[Display(Name = "First Name")]
public required string FirstName { get; set; }

[Required(ErrorMessage = "Mobile number is required")]
[SingaporeMobile]
[Display(Name = "Mobile Number")]
public required string Mobile { get; set; }

[Required(ErrorMessage = "Email is required")]
[EmailAddress(ErrorMessage = "Please enter a valid email address")]
[NoHtml]
[StringLength(100, ErrorMessage = "Email cannot exceed 100 characters")]
public required string Email { get; set; }
```

**B. Client-Side Validation (JavaScript)**

**Features:**
- Real-time validation feedback
- Input formatting (credit card, mobile)
- XSS/SQL injection pattern detection
- Visual feedback (green/red borders)

**Implementation:**
```javascript
// wwwroot/js/input-validation.js
InputValidation.validateEmail(email);
InputValidation.validateMobile(mobile);
InputValidation.validateName(name, fieldName);
InputValidation.validateAddress(address, fieldName);
InputValidation.validateCreditCard(cardNumber);
```

**Auto-Formatting:**
```javascript
formatCreditCard(input);  // 1234 5678 9012 3456
formatMobile(input);      // 81234567
```

### 6. Proper Encoding Before Database Storage ?

**A. Sensitive Data Encryption**
```csharp
// Encrypt before saving
user.CreditCard = _encryptionService.Encrypt(sanitizedCreditCard);

// Decrypt when reading
var decryptedCard = _encryptionService.Decrypt(user.CreditCard);
```

**B. Text Data Encoding**
- All text data is sanitized before storage
- HTML entities decoded/encoded appropriately
- UTF-8 encoding for database

**C. Password Hashing**
```csharp
// ASP.NET Core Identity automatically hashes passwords
await _userManager.CreateAsync(user, password);
// Password is hashed with PBKDF2, never stored as plain text
```

### 7. Error and Warning Messages ?

**A. Server-Side Error Messages**

**ModelState Errors:**
```csharp
// Field-specific errors
ModelState.AddModelError("RModel.Email", "Invalid email format.");

// General errors
ModelState.AddModelError(string.Empty, "Registration failed.");
```

**Display in Razor:**
```html
<!-- All errors -->
<div asp-validation-summary="All" class="text-danger"></div>

<!-- Field-specific errors -->
<span asp-validation-for="RModel.Email" class="text-danger"></span>
```

**B. Client-Side Error Messages**

**Real-Time Validation:**
```javascript
// Show error
InputValidation.showError(inputElement, "Email is required");

// Show success
InputValidation.showSuccess(inputElement);

// Clear validation
InputValidation.clearValidation(inputElement);
```

**Visual Feedback:**
- ? Green border + checkmark for valid input
- ? Red border + error message for invalid input
- ?? Real-time validation on blur event

### 8. Security Headers ?

**Implemented in Program.cs:**

```csharp
// Content Security Policy - prevents XSS
Content-Security-Policy: default-src 'self'; script-src 'self' 'unsafe-inline' ...

// Prevent MIME sniffing
X-Content-Type-Options: nosniff

// Prevent clickjacking
X-Frame-Options: SAMEORIGIN

// Enable XSS filtering
X-XSS-Protection: 1; mode=block

// Control referrer information
Referrer-Policy: strict-origin-when-cross-origin

// Control browser features
Permissions-Policy: geolocation=(), microphone=(), camera=()
```

## Validation Rules Summary

### Email Validation
- ? Required
- ? Valid email format (RFC 5322)
- ? Maximum 100 characters
- ? No HTML tags
- ? No XSS patterns

### Password Validation
- ? Required
- ? Minimum 12 characters
- ? At least 1 lowercase letter
- ? At least 1 uppercase letter
- ? At least 1 number
- ? At least 1 special character
- ? No common patterns
- ? No username in password

### Name Validation (First/Last Name)
- ? Required
- ? 2-50 characters
- ? Letters, spaces, hyphens, apostrophes only
- ? No HTML tags
- ? No XSS patterns
- ? No consecutive special characters (3+)

### Mobile Number Validation
- ? Required
- ? Exactly 8 digits
- ? Starts with 8 or 9
- ? Singapore format only
- ? Auto-formatted (removes spaces/dashes)

### Address Validation (Billing/Shipping)
- ? Required
- ? 5-200 characters
- ? Alphanumeric + common punctuation (.,#-/)
- ? No HTML tags
- ? No XSS patterns

### Credit Card Validation
- ? Required
- ? 13-19 digits
- ? Valid Luhn checksum
- ? Auto-formatted with spaces
- ? Encrypted before storage

### Photo Upload Validation
- ? Required
- ? JPG/JPEG only (extension check)
- ? Content-Type validation
- ? Maximum 5MB file size
- ? Unique filename (GUID)

## Testing the Implementation

### 1. SQL Injection Testing

**Test Cases:**
```
Email: admin'--
Email: ' OR '1'='1
Email: '; DROP TABLE Users--
```

**Expected Result:**
- ? Validation error: "Invalid characters detected"
- ? Request rejected before database query

### 2. XSS Testing

**Test Cases:**
```
Name: <script>alert('XSS')</script>
Email: javascript:alert('XSS')
Address: <img src=x onerror=alert('XSS')>
```

**Expected Result:**
- ? Validation error: "HTML tags are not allowed" or "Invalid characters detected"
- ? If somehow saved, output is HTML-encoded

### 3. CSRF Testing

**Test Cases:**
- Submit form without anti-forgery token
- Submit form from external site

**Expected Result:**
- ? HTTP 400 Bad Request
- ? Anti-forgery token validation failed

### 4. Input Validation Testing

**Test Cases:**
```
Email: invalid-email
Mobile: 1234567 (wrong length)
Mobile: 71234567 (wrong prefix)
Name: John123 (numbers not allowed)
Password: short (too short)
Credit Card: 1234 (invalid Luhn)
```

**Expected Result:**
- ? Client-side validation error (immediate feedback)
- ? Server-side validation error (if client-side bypassed)

## File Structure

```
Services/
??? IInputSanitizationService.cs      (Interface)
??? InputSanitizationService.cs       (Implementation)

Attributes/
??? NoHtmlAttribute.cs                (Anti-XSS)
??? NoSqlInjectionAttribute.cs        (Anti-SQL Injection)
??? NameValidationAttribute.cs        (Name format)
??? SingaporeMobileAttribute.cs       (Mobile format)
??? AddressValidationAttribute.cs     (Address format)
??? StrongPasswordAttribute.cs        (Password strength)

ViewModels/
??? Register.cs                       (With validation attributes)
??? Login.cs                          (With validation attributes)

wwwroot/js/
??? input-validation.js               (Client-side validation)
??? password-strength.js              (Password strength indicator)

Pages/
??? Register.cshtml                   (With client-side validation)
??? Register.cshtml.cs                (With sanitization)
??? Login.cshtml                      (With client-side validation)
??? Login.cshtml.cs                   (With sanitization)

Program.cs                            (Service registration, security headers)
```

## Security Checklist

- ? SQL Injection Prevention (Entity Framework parameterized queries)
- ? XSS Prevention (HTML encoding, CSP headers, input validation)
- ? CSRF Protection (Anti-forgery tokens)
- ? Input Sanitization (Server-side service)
- ? Input Validation (Client and server-side)
- ? Proper Encoding (HTML encoding, data encryption)
- ? Error Messages (Field-specific and general)
- ? Security Headers (CSP, X-Frame-Options, etc.)
- ? File Upload Validation (Type, size, content)
- ? Password Strength (12+ chars, complexity requirements)
- ? Rate Limiting (Account lockout after 3 failed attempts)
- ? Session Management (Secure cookies, timeout)
- ? Audit Logging (User activities tracked)
- ? Data Encryption (Credit card encrypted in database)
- ? HTTPS Enforcement (All traffic over HTTPS)

## Best Practices Followed

1. **Defense in Depth**: Multiple layers of protection
2. **Principle of Least Privilege**: Minimal permissions granted
3. **Secure by Default**: Security features enabled by default
4. **Fail Securely**: Errors don't expose sensitive information
5. **Don't Trust Client Input**: All input validated server-side
6. **Keep It Simple**: Simple, maintainable code
7. **Log Security Events**: All security-relevant events logged
8. **Use Framework Features**: Leverage ASP.NET Core security features

## Conclusion

All required input validation and security measures have been implemented:

? **Prevent Injection** - SQL Injection and XSS attacks prevented through multiple layers
? **Proper Input Sanitation** - All user input sanitized and validated
? **Client & Server Validation** - Dual validation for better UX and security
? **Error Messages** - Clear, helpful error messages displayed
? **Proper Encoding** - All data properly encoded before storage and display
? **CSRF Protection** - Anti-forgery tokens protect against CSRF attacks

The application follows industry best practices and security standards (OWASP Top 10, NIST guidelines).
