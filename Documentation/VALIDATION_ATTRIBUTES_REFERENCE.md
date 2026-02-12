# Quick Reference: Custom Validation Attributes

## Available Validation Attributes

### 1. NameValidationAttribute
**Purpose**: Validates name fields (First Name, Last Name)

**Rules**:
- ? Required
- ? 2-50 characters
- ? Letters, spaces, hyphens (-), apostrophes (') only
- ? No HTML tags
- ? No XSS patterns
- ? No more than 2 consecutive special characters

**Usage**:
```csharp
[Required(ErrorMessage = "First name is required")]
[NameValidation]
public string FirstName { get; set; }
```

**Valid Examples**:
- "John"
- "Mary-Jane"
- "O'Brien"
- "Jean-Pierre"
- "José María"

**Invalid Examples**:
- "J" (too short)
- "John123" (contains numbers)
- "John<script>" (contains HTML)
- "---" (too many consecutive special chars)

---

### 2. SingaporeMobileAttribute
**Purpose**: Validates Singapore mobile phone numbers

**Rules**:
- ? Exactly 8 digits
- ? Must start with 8 or 9
- ? Spaces and dashes automatically removed

**Usage**:
```csharp
[Required(ErrorMessage = "Mobile number is required")]
[SingaporeMobile]
public string Mobile { get; set; }
```

**Valid Examples**:
- "81234567"
- "91234567"
- "8123-4567" (auto-formatted)
- "8123 4567" (auto-formatted)

**Invalid Examples**:
- "1234567" (7 digits)
- "123456789" (9 digits)
- "71234567" (doesn't start with 8/9)
- "abcd1234" (contains letters)

---

### 3. AddressValidationAttribute
**Purpose**: Validates address fields (Billing, Shipping)

**Rules**:
- ? Required
- ? 5-200 characters
- ? Letters, numbers, spaces, and punctuation: . , - # / ( )
- ? No HTML tags
- ? No XSS patterns

**Usage**:
```csharp
[Required(ErrorMessage = "Billing address is required")]
[AddressValidation]
public string Billing { get; set; }
```

**Valid Examples**:
- "123 Main Street, Singapore"
- "456 Orchard Road, #01-234"
- "Unit 5/10, Building A"
- "Blk 123, Ang Mo Kio Ave 3, #04-567"

**Invalid Examples**:
- "123" (too short)
- "123 Main@Street" (@ not allowed)
- "123 Main Street<script>" (contains HTML)
- [200+ characters] (too long)

---

### 4. NoHtmlAttribute
**Purpose**: Prevents HTML and script content

**Rules**:
- ? No HTML tags (< >)
- ? No JavaScript protocol
- ? No event handlers (onerror, onclick, etc.)
- ? No script tags
- ? No iframe tags

**Usage**:
```csharp
[Required(ErrorMessage = "Email is required")]
[EmailAddress]
[NoHtml]
public string Email { get; set; }
```

**Valid Examples**:
- "test@example.com"
- "Normal text input"
- "Product name 123"

**Invalid Examples**:
- "test<b>bold</b>@example.com"
- "<script>alert('XSS')</script>"
- "javascript:alert('XSS')"
- "Text with <div>tags</div>"

---

### 5. NoSqlInjectionAttribute
**Purpose**: Detects SQL injection patterns (defense in depth)

**Rules**:
- ? No SQL keywords (SELECT, INSERT, UPDATE, DELETE, etc.)
- ? No SQL comments (-- or /* */)
- ? No SQL boolean conditions ('1'='1')
- ? No stored procedure names (xp_, sp_)

**Usage**:
```csharp
[Required]
[NoSqlInjection]
public string SearchTerm { get; set; }
```

**Valid Examples**:
- "Normal search term"
- "Product ABC-123"
- "test@example.com"

**Invalid Examples**:
- "admin'--"
- "' OR '1'='1"
- "'; DROP TABLE Users--"
- "SELECT * FROM Users"

**Note**: This is secondary protection. Primary protection is Entity Framework parameterized queries.

---

### 6. StrongPasswordAttribute
**Purpose**: Enforces strong password requirements

**Rules**:
- ? Minimum 12 characters
- ? At least 1 lowercase letter (a-z)
- ? At least 1 uppercase letter (A-Z)
- ? At least 1 number (0-9)
- ? At least 1 special character

**Usage**:
```csharp
[Required(ErrorMessage = "Password is required")]
[DataType(DataType.Password)]
[StrongPassword]
public string Password { get; set; }
```

**Valid Examples**:
- "MyP@ssw0rd123"
- "Str0ng!Pass#2024"
- "SecureP@ss123"

**Invalid Examples**:
- "Pass123!" (too short)
- "password123!" (no uppercase)
- "PASSWORD123!" (no lowercase)
- "Password!@#" (no number)
- "Password1234" (no special char)

---

## Combining Attributes

You can combine multiple attributes for comprehensive validation:

```csharp
[Required(ErrorMessage = "Email is required")]
[EmailAddress(ErrorMessage = "Please enter a valid email address")]
[NoHtml]
[NoSqlInjection]
[StringLength(100, ErrorMessage = "Email cannot exceed 100 characters")]
[Display(Name = "Email Address")]
public string Email { get; set; }
```

---

## Built-in Attributes Used

### [Required]
Ensures field is not empty

```csharp
[Required(ErrorMessage = "This field is required")]
public string FieldName { get; set; }
```

### [EmailAddress]
Validates email format

```csharp
[EmailAddress(ErrorMessage = "Please enter a valid email address")]
public string Email { get; set; }
```

### [CreditCard]
Validates credit card format (Luhn algorithm)

```csharp
[CreditCard(ErrorMessage = "Please enter a valid credit card number")]
public string CreditCard { get; set; }
```

### [StringLength]
Enforces minimum and maximum length

```csharp
[StringLength(100, MinimumLength = 5, ErrorMessage = "Must be 5-100 characters")]
public string FieldName { get; set; }
```

### [Compare]
Ensures two fields match (e.g., password confirmation)

```csharp
[Compare("Password", ErrorMessage = "Passwords do not match")]
public string ConfirmPassword { get; set; }
```

### [DataType]
Specifies the data type for better UX

```csharp
[DataType(DataType.Password)]
public string Password { get; set; }

[DataType(DataType.Date)]
public DateTime DateOfBirth { get; set; }
```

### [Display]
Specifies the display name for the field

```csharp
[Display(Name = "First Name")]
public string FirstName { get; set; }
```

---

## Error Message Customization

All custom attributes support custom error messages:

```csharp
[NameValidation(ErrorMessage = "Invalid name format. Please use only letters.")]
public string FirstName { get; set; }

[SingaporeMobile(ErrorMessage = "Please enter a valid Singapore mobile number.")]
public string Mobile { get; set; }
```

---

## Client-Side Validation Functions

Corresponding JavaScript validation functions in `wwwroot/js/input-validation.js`:

```javascript
// Email validation
InputValidation.validateEmail(email)

// Mobile number validation
InputValidation.validateMobile(mobile)

// Name validation
InputValidation.validateName(name, "First name")

// Address validation
InputValidation.validateAddress(address, "Billing address")

// Credit card validation
InputValidation.validateCreditCard(cardNumber)

// XSS detection
InputValidation.containsXss(input)

// SQL injection detection
InputValidation.containsSqlInjection(input)

// Show validation error
InputValidation.showError(inputElement, message)

// Show validation success
InputValidation.showSuccess(inputElement)
```

---

## Common Patterns

### Registration Form
```csharp
public class Register
{
    [Required(ErrorMessage = "First name is required")]
    [NameValidation]
    [Display(Name = "First Name")]
    public string FirstName { get; set; }

    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email address")]
    [NoHtml]
    [StringLength(100)]
    [Display(Name = "Email Address")]
    public string Email { get; set; }

    [Required(ErrorMessage = "Mobile is required")]
    [SingaporeMobile]
    [Display(Name = "Mobile Number")]
    public string Mobile { get; set; }

    [Required(ErrorMessage = "Address is required")]
    [AddressValidation]
    [Display(Name = "Billing Address")]
    public string Billing { get; set; }

    [Required(ErrorMessage = "Password is required")]
    [DataType(DataType.Password)]
    [StrongPassword]
    [Display(Name = "Password")]
    public string Password { get; set; }
}
```

### Search Form
```csharp
public class SearchModel
{
    [Required]
    [NoHtml]
    [NoSqlInjection]
    [StringLength(100)]
    [Display(Name = "Search Term")]
    public string Query { get; set; }
}
```

---

## Testing Your Validation

1. **Valid Input**: Should pass validation, green border
2. **Invalid Input**: Should show error message, red border
3. **Client-Side**: Real-time feedback on blur
4. **Server-Side**: Final validation before processing

**Example Test**:
```
Input: John<script>
Expected: ? Error: "Name can only contain letters, spaces, hyphens, and apostrophes"
```

---

## Need Help?

- Check `INPUT_VALIDATION_IMPLEMENTATION_GUIDE.md` for detailed implementation
- Check `INPUT_VALIDATION_TESTING_GUIDE.md` for test scenarios
- Check `INPUT_VALIDATION_SUMMARY.md` for overview
