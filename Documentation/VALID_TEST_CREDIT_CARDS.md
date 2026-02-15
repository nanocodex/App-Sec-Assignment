# Valid Test Credit Card Numbers

## ?? Important Notice

The credit card validation in this application uses the **Luhn algorithm** (also known as the mod-10 algorithm) to verify that credit card numbers are mathematically valid. Both client-side JavaScript and server-side .NET validation enforce this.

## ? Valid Test Credit Card Numbers

These numbers **pass both validations** and can be used for testing:

### Recommended for Quick Testing
```
4111 1111 1111 1111
```
**Type:** Visa (16 digits)  
**Status:** ? Passes Luhn algorithm  
**Use Case:** Best for general testing - easy to remember

---

### All Valid Test Numbers

| Card Type | Card Number | Digits | Luhn Valid |
|-----------|-------------|--------|------------|
| **Visa** | `4111 1111 1111 1111` | 16 | ? |
| **Visa** | `4012 8888 8888 1881` | 16 | ? |
| **Visa** | `4539 1488 0343 6467` | 16 | ? |
| **Mastercard** | `5425 2334 3010 9903` | 16 | ? |
| **Mastercard** | `5105 1051 0510 5100` | 16 | ? |
| **American Express** | `3782 822463 10005` | 15 | ? |
| **American Express** | `3714 496353 98431` | 15 | ? |
| **Discover** | `6011 1111 1111 1117` | 16 | ? |
| **Discover** | `6011 0009 9013 9424` | 16 | ? |
| **JCB** | `3530 1113 3330 0000` | 16 | ? |

---

## ? Invalid Examples (For Negative Testing)

These numbers will **fail validation**:

| Card Number | Why It Fails |
|-------------|--------------|
| `1234 5678 9012 3456` | Invalid Luhn checksum |
| `4532 0151 1416 6950` | Invalid Luhn checksum (was in old docs) ? |
| `4532 1488 0343 6467` | Invalid Luhn checksum (incorrect) ? |
| `1234 5678` | Too short (< 13 digits) |
| `1234 5678 9012 3456 7890 1234` | Too long (> 19 digits) |

---

## ?? How Luhn Algorithm Works

The Luhn algorithm validates that a credit card number is mathematically valid:

1. Starting from the rightmost digit, double every second digit
2. If doubling results in a number > 9, subtract 9
3. Sum all the digits
4. If the sum is divisible by 10 (ends in 0), the number is valid

### Example: Validating `4111 1111 1111 1111`

```
Original: 4 1 1 1 1 1 1 1 1 1 1 1 1 1 1 1
          ? ? ? ? ? ? ? ? ? ? ? ? ? ? ? ?
Step 1:   4 2 1 2 1 2 1 2 1 2 1 2 1 2 1 2
          (every 2nd digit from right is doubled)

Step 2:   4 2 1 2 1 2 1 2 1 2 1 2 1 2 1 2
          (none > 9, so no changes)

Step 3:   4+2+1+2+1+2+1+2+1+2+1+2+1+2+1+2 = 30

Step 4:   30 % 10 = 0 ? VALID!
```

---

## ?? Testing in the Application

### Registration Form
1. Navigate to `/Register`
2. Fill in all required fields
3. For Credit Card, use: `4111 1111 1111 1111`
4. The number will be:
   - ? Validated by client-side JavaScript (Luhn check)
   - ? Auto-formatted with spaces as you type
   - ? Validated by server-side `[CreditCard]` attribute
   - ? Encrypted before storage in database
   - ? Displayed with masking (last 4 digits only)

### Testing Invalid Cards
To test validation errors, try:
- **Too short**: `1234 5678`
- **Invalid Luhn**: `1234 5678 9012 3456`
- **Non-numeric**: `abcd efgh ijkl mnop`

### Expected Behavior

**Valid Card Entry:**
```
Input:  4111 1111 1111 1111
Result: ? Green border
        ? No error message
        ? Ready to submit
```

**Invalid Card Entry:**
```
Input:  1234 5678 9012 3456
Result: ? Red border
        ? Error: "Please enter a valid credit card number"
        ? Form submission blocked
```

---

## ?? Security Notes

1. **Encryption**: All credit card numbers are encrypted using ASP.NET Core Data Protection API before storage
2. **Masking**: Only last 4 digits shown on user profile page
3. **No Logging**: Credit card numbers are never logged in plain text
4. **HTTPS Only**: Credit card data only transmitted over HTTPS
5. **PCI Compliance**: These are test numbers only - never use real credit cards in development

---

## ?? References

- [Luhn Algorithm (Wikipedia)](https://en.wikipedia.org/wiki/Luhn_algorithm)
- [Test Credit Card Numbers (PayPal)](https://developer.paypal.com/api/nvp-soap/payflow/integration-guide/test-transactions/#standard-test-cards)
- [PCI DSS Compliance](https://www.pcisecuritystandards.org/)

---

## ?? Troubleshooting

### Problem: "Credit card number is invalid" error

**Solution:** Make sure you're using one of the valid test numbers above. The old documentation had incorrect numbers (`4532 0151 1416 6950` and `4532 1488 0343 6467`) that fail Luhn validation.

**Quick Fix:** Use `4111 1111 1111 1111` instead (easy to remember - all 1s after the 4).

### Problem: Number not auto-formatting with spaces

**Solution:** Check that `input-validation.js` is loaded correctly. The `formatCreditCard()` function should trigger on input event.

### Problem: Server-side validation fails but client-side passes

**Solution:** This shouldn't happen if using the test numbers above. If it does, check:
1. The `[CreditCard]` attribute is applied to the property
2. The number hasn't been modified during sanitization
3. Spaces are properly stripped before validation

---

## ? Quick Test Checklist

- [ ] Use valid test number: `4111 1111 1111 1111`
- [ ] Client-side validation passes (green border)
- [ ] Auto-formatting adds spaces (4111 1111 1111 1111)
- [ ] Server-side validation passes (no ModelState error)
- [ ] Number is encrypted before storage
- [ ] Only last 4 digits shown on profile page

---

**Last Updated:** January 2024  
**Version:** 1.0
