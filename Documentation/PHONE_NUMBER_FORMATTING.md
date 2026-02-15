# Phone Number Formatting with Country Codes

## Overview
The phone number field now supports both local Singapore mobile numbers and international formats with country codes. The field automatically formats the number with appropriate spacing based on the country code.

## Supported Formats

### Singapore (No Country Code)
- **Input**: `97593160`
- **Output**: `97593160` (no spacing)
- **Validation**: Must be exactly 8 digits starting with 8 or 9

### Singapore (+65)
- **Input**: `+6597593160`
- **Output**: `+65 9759 3160`
- **Format**: `+65 XXXX XXXX`
- **Validation**: +65 followed by 8 digits starting with 8 or 9

### Malaysia (+60)
- **Input**: `+60123456789`
- **Output**: `+60 12 345 6789`
- **Format**: `+60 XX XXX XXXX`
- **Validation**: +60 followed by 9-10 digits

### Indonesia (+62)
- **Input**: `+6281234567890`
- **Output**: `+62 812 3456 7890`
- **Format**: `+62 XXX XXXX XXXX`
- **Validation**: +62 followed by 9-12 digits

## Implementation Details

### Client-Side (JavaScript)
Location: `wwwroot/js/input-validation.js`

**Auto-Formatting Function**:
```javascript
window.formatMobile = function (input) {
    // Formats phone numbers based on country code
    // Singapore: +65 XXXX XXXX
    // Malaysia: +60 XX XXX XXXX
    // Indonesia: +62 XXX XXXX XXXX
    // No country code: XXXXXXXX (no spacing)
}
```

**Validation Function**:
```javascript
InputValidation.validateMobile = function (mobile) {
    // Validates format based on country code
    // Returns: { valid: boolean, message: string }
}
```

### Server-Side (C#)
Location: `Attributes/SingaporeMobileAttribute.cs`

**Validation Rules**:
- No country code: Must be 8 digits starting with 8 or 9
- +65 (Singapore): Must be followed by 8 digits starting with 8 or 9
- +60 (Malaysia): Must be followed by 9-10 digits
- +62 (Indonesia): Must be followed by 9-12 digits

## Usage Examples

### Valid Inputs
? `81234567` ? `81234567`
? `97593160` ? `97593160`
? `+6597593160` ? `+65 9759 3160`
? `+6581234567` ? `+65 8123 4567`
? `+60123456789` ? `+60 12 345 6789`
? `+6281234567890` ? `+62 812 3456 7890`

### Invalid Inputs
? `71234567` - Does not start with 8 or 9
? `9123456` - Only 7 digits (need 8)
? `+6571234567` - Singapore number must start with 8 or 9
? `+1234567890` - Unsupported country code

## User Experience

1. **Real-Time Formatting**: As the user types, the number is automatically formatted with appropriate spacing
2. **Visual Feedback**: Valid numbers show a green border, invalid numbers show a red border
3. **Helpful Messages**: Clear error messages explain what format is expected
4. **Flexible Input**: Users can type with or without spaces - the system handles both

## Testing

To test the phone number formatting:

1. Go to the registration page
2. Try entering these test cases in the mobile number field:
   - `97593160` (should remain as-is)
   - `+6597593160` (should format to `+65 9759 3160`)
   - `+60123456789` (should format to `+60 12 345 6789`)
   - `+6281234567890` (should format to `+62 812 3456 7890`)
3. Observe the automatic formatting as you type
4. Tab out of the field to see validation feedback

## Notes

- Spaces are removed during validation, so users can enter numbers with or without spaces
- The formatting happens in real-time as the user types
- Both client-side (JavaScript) and server-side (C#) validation ensure data integrity
- Only digits and the + symbol are allowed (at the start)
- Maximum length is enforced to prevent excessively long inputs
