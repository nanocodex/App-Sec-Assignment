# Quick Reference: Address Field Security

## What Changed?

**Before:** Addresses could only contain letters, numbers, and limited punctuation (.,#-/)  
**Now:** Addresses can contain ALL special characters (including $%^&*) except XSS attack patterns

---

## Allowed Characters

### ? **Now Accepted in Addresses:**
- Dollar signs: `$`
- Percent: `%`
- Caret: `^`
- Ampersand: `&`
- Asterisk: `*`
- Parentheses: `(`, `)`
- Brackets: `{`, `}`, `[`, `]`
- Plus/Minus: `+`, `-`
- Equals: `=`
- Tilde: `~`
- Backtick: `` ` ``
- Exclamation: `!`
- At: `@`
- Hash: `#`
- All other printable characters

### ? **Still Blocked (Security):**
- Script tags: `<script>...</script>`
- JavaScript protocol: `javascript:`
- Event handlers: `onerror=`, `onclick=`, etc.
- Iframes: `<iframe>...</iframe>`
- Other XSS patterns
- Control characters (null bytes, etc.)

---

## Example Valid Addresses

```
? $100 Dollar Street
? Unit #5-67, Building A
? 50% Discount Lane
? AT&T Corporate Center
? C++ Programming Plaza
? **VIP** Address
? John's (Main) House
? 123 Main St. & Co.
? $%^&* Test Street
```

---

## Example Blocked Addresses (XSS Attempts)

```
? <script>alert('XSS')</script>
? javascript:alert('XSS')
? <img src=x onerror=alert('XSS')>
? <iframe src="evil.com"></iframe>
? onclick=alert('XSS')
```

---

## How It Works

### Registration Flow:

```
1. User types: "$100 Main St"
2. Client validates: ? No XSS patterns
3. Server validates: ? No dangerous patterns
4. Sanitize: Remove control chars ? "$100 Main St"
5. HTML Encode: Convert to entities ? "&#x24;100 Main St"
6. Store in database: "&#x24;100 Main St"
7. Display with Html.Raw(): User sees "$100 Main St"
```

### Security Layers:

```
???????????????????????????????????????
? Client Validation (JavaScript)      ? ? Blocks XSS patterns
???????????????????????????????????????
? Server Validation (Attribute)       ? ? Blocks XSS patterns
???????????????????????????????????????
? Input Sanitization                  ? ? Removes control chars
???????????????????????????????????????
? HTML Encoding                       ? ? Encodes special chars
???????????????????????????????????????
? Database Storage                    ? ? Stores encoded values
???????????????????????????????????????
? Display with Html.Raw()             ? ? Decodes for display
???????????????????????????????????????
```

---

## For Developers

### Key Files Modified:

1. **`Attributes/AddressValidationAttribute.cs`**
   - Removed strict regex
   - Added XSS pattern detection
   - Allows all printable characters

2. **`wwwroot/js/input-validation.js`**
   - Updated `validateAddress()` function
   - Removed character restriction
   - Added XSS pattern checks

3. **`Pages/Register.cshtml.cs`**
   - Added HTML encoding:
   ```csharp
   var encodedBilling = _sanitizationService.SanitizeHtml(sanitizedBilling);
   var encodedShipping = _sanitizationService.SanitizeHtml(sanitizedShipping);
   ```

4. **`Pages/Index.cshtml`**
   - Changed to `@Html.Raw()`:
   ```razor
   <div>@Html.Raw(Model.CurrentUser.Billing)</div>
   <div>@Html.Raw(Model.CurrentUser.Shipping)</div>
   ```

### Key Service Methods:

```csharp
// Remove control characters
_sanitizationService.SanitizeInput(input);

// HTML encode special characters
_sanitizationService.SanitizeHtml(input);

// Check for XSS patterns
_sanitizationService.ContainsPotentialXss(input);
```

---

## Testing Checklist

### ? Test Valid Special Characters:

```bash
# Register with these addresses:
Billing: $100 Dollar Street
Shipping: Unit #5-67 & Co.
```

**Expected:** 
- Registration succeeds
- Addresses display correctly on profile
- Database shows HTML entities

### ? Test XSS Protection:

```bash
# Try to register with:
Billing: <script>alert('XSS')</script>
```

**Expected:**
- Validation error message
- Registration fails
- No data stored

### ? Test Migration:

```bash
# Navigate to /DataMigration
# Click "Run Address Encoding Migration"
```

**Expected:**
- Existing plaintext records encoded
- Special characters preserved
- Display unchanged for users

---

## Database Format

### Before Encoding:
```sql
SELECT Billing, Shipping FROM AspNetUsers WHERE Email = 'test@example.com';
-- Billing: $100 Main St
-- Shipping: Unit #5-67
```

### After Encoding:
```sql
SELECT Billing, Shipping FROM AspNetUsers WHERE Email = 'test@example.com';
-- Billing: &#x24;100 Main St
-- Shipping: Unit &#x23;5-67
```

### Display on Web Page:
```html
<!-- Rendered HTML -->
<div>$100 Main St</div>
<div>Unit #5-67</div>
```

---

## Character Encoding Reference

| Character | HTML Entity | Description |
|-----------|-------------|-------------|
| `<` | `&lt;` | Less than |
| `>` | `&gt;` | Greater than |
| `&` | `&amp;` | Ampersand |
| `"` | `&quot;` | Quote |
| `'` | `&#x27;` or `&#39;` | Apostrophe |
| `$` | `&#x24;` | Dollar sign |
| `%` | `&#x25;` | Percent |
| `^` | `&#x5E;` | Caret |
| `*` | `&#x2A;` | Asterisk |
| `#` | `&#x23;` | Hash |
| `(` | `&#x28;` | Left paren |
| `)` | `&#x29;` | Right paren |

---

## Troubleshooting

### Issue: Addresses show as HTML entities (e.g., `&#x24;`)

**Cause:** Using `@Model.Field` instead of `@Html.Raw(Model.Field)`

**Solution:** Update the Razor view:
```razor
<!-- Wrong -->
<div>@Model.CurrentUser.Billing</div>

<!-- Correct -->
<div>@Html.Raw(Model.CurrentUser.Billing)</div>
```

### Issue: Validation blocks legitimate addresses

**Cause:** XSS pattern false positive

**Solution:** Check if address contains patterns like:
- `javascript:`
- `<script`
- `onerror=`

If legitimate, contact developer to whitelist pattern.

### Issue: Special characters not saved

**Cause:** Missing HTML encoding step

**Solution:** Verify `Register.cshtml.cs` has:
```csharp
var encodedBilling = _sanitizationService.SanitizeHtml(sanitizedBilling);
```

---

## Security Notes

### Why `@Html.Raw()` is Safe Here:

1. **Data is already encoded** in the database
2. **Validation blocks** XSS patterns before storage
3. **HTML encoding** prevents script execution
4. **Display just decodes** the safe entities

**Without encoding:**
```
User Input: <script>alert('XSS')</script>
Stored: <script>alert('XSS')</script>
Display: [SCRIPT EXECUTES! ?]
```

**With encoding:**
```
User Input: <script>alert('XSS')</script>
Validation: ? BLOCKED
OR
Stored: &lt;script&gt;alert(&#x27;XSS&#x27;)&lt;/script&gt;
Display with Html.Raw(): <script>alert('XSS')</script> as TEXT ?
```

---

## Quick Commands

### Build Project:
```bash
dotnet build
```

### Run Application:
```bash
dotnet run
```

### Run Migration:
```
Navigate to: https://localhost:XXXX/DataMigration
Click: "Run Address Encoding Migration"
```

### Check Database:
```sql
SELECT Email, Billing, Shipping 
FROM AspNetUsers 
ORDER BY Email;
```

---

## Summary

| Feature | Status |
|---------|--------|
| **Allow special characters** | ? Yes |
| **Prevent XSS attacks** | ? Yes |
| **HTML encoding** | ? Automatic |
| **Display correctly** | ? Html.Raw() |
| **Migration tool** | ? Works |
| **Build status** | ? Success |

**You can now register users with addresses containing any special characters while maintaining full XSS protection!** ??
