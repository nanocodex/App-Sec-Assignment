// Client-side input validation and sanitization
(function () {
    'use strict';

    // Validation patterns
    const patterns = {
        email: /^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$/,
        singaporeMobile: /^[89]\d{7}$/,
        internationalMobile: /^\+\d{1,3}\d{8,12}$/,
        name: /^[\p{L}\s'\-]+$/u,
        noHtml: /<[^>]+>/,
        xssPatterns: [
            /<script/i,
            /javascript:/i,
            /onerror\s*=/i,
            /onload\s*=/i,
            /onclick\s*=/i,
            /onmouseover\s*=/i
        ],
        sqlPatterns: [
            /(\b(SELECT|INSERT|UPDATE|DELETE|DROP|CREATE|ALTER|EXEC|EXECUTE|UNION|DECLARE)\b)/i,
            /(--|;|\/\*|\*\/|xp_|sp_)/,
            /'?\s*(OR|AND)\s*'?\s*\d+\s*=\s*\d+/i
        ]
    };

    // Real-time validation functions
    window.InputValidation = {
        // Validate email format
        validateEmail: function (email) {
            if (!email) return { valid: false, message: 'Email is required' };
            
            email = email.trim();
            
            if (!patterns.email.test(email)) {
                return { valid: false, message: 'Please enter a valid email address' };
            }
            
            if (email.length > 100) {
                return { valid: false, message: 'Email cannot exceed 100 characters' };
            }
            
            if (this.containsXss(email)) {
                return { valid: false, message: 'Invalid characters detected' };
            }
            
            return { valid: true, message: '' });
        },

        // Validate Singapore mobile number
        validateMobile: function (mobile) {
            if (!mobile) return { valid: false, message: 'Mobile number is required' };
            
            mobile = mobile.trim();
            
            // Remove spaces for validation
            const mobileNoSpaces = mobile.replace(/\s/g, '');
            
            // Check if it starts with +
            if (mobileNoSpaces.startsWith('+')) {
                // International format
                if (!patterns.internationalMobile.test(mobileNoSpaces)) {
                    return { valid: false, message: 'Invalid international mobile number format' };
                }
                
                // Validate specific country codes
                if (mobileNoSpaces.startsWith('+65')) {
                    // Singapore: +65 followed by 8 digits
                    if (!/^\+65\d{8}$/.test(mobileNoSpaces)) {
                        return { valid: false, message: 'Singapore mobile number must be +65 followed by 8 digits' };
                    }
                } else if (mobileNoSpaces.startsWith('+60')) {
                    // Malaysia: +60 followed by 9-10 digits
                    if (!/^\+60\d{9,10}$/.test(mobileNoSpaces)) {
                        return { valid: false, message: 'Malaysia mobile number must be +60 followed by 9-10 digits' };
                    }
                } else if (mobileNoSpaces.startsWith('+62')) {
                    // Indonesia: +62 followed by 9-12 digits
                    if (!/^\+62\d{9,12}$/.test(mobileNoSpaces)) {
                        return { valid: false, message: 'Indonesia mobile number must be +62 followed by 9-12 digits' };
                    }
                }
            } else {
                // Singapore format without country code
                if (!patterns.singaporeMobile.test(mobileNoSpaces)) {
                    return { valid: false, message: 'Mobile number must be 8 digits and start with 8 or 9, or include country code' };
                }
            }
            
            return { valid: true, message: '' };
        },

        // Validate name (first name, last name)
        validateName: function (name, fieldName) {
            if (!name) return { valid: false, message: `${fieldName} is required` };
            
            name = name.trim();
            
            if (name.length < 2) {
                return { valid: false, message: `${fieldName} must be at least 2 characters long` };
            }
            
            if (name.length > 50) {
                return { valid: false, message: `${fieldName} cannot exceed 50 characters` };
            }
            
            if (!patterns.name.test(name)) {
                return { valid: false, message: `${fieldName} can only contain letters, spaces, hyphens, and apostrophes` };
            }
            
            if (this.containsXss(name)) {
                return { valid: false, message: 'Invalid characters detected' };
            }
            
            return { valid: true, message: '' };
        },

        // Validate address - allows all special characters except dangerous script patterns
        validateAddress: function (address, fieldName) {
            if (!address) return { valid: false, message: `${fieldName} is required` };
            
            address = address.trim();
            
            if (address.length < 5) {
                return { valid: false, message: 'Address is too short. Please enter a complete address' };
            }
            
            if (address.length > 200) {
                return { valid: false, message: 'Address cannot exceed 200 characters' };
            }
            
            // Check for control characters (except newline, carriage return, tab)
            if (/[\x00-\x08\x0B\x0C\x0E-\x1F\x7F]/.test(address)) {
                return { valid: false, message: 'Address contains invalid control characters' };
            }
            
            // Only block obvious XSS attack patterns, allow all other special characters
            const xssPatterns = [
                /<script[\s\S]*?>[\s\S]*?<\/script>/i,
                /javascript\s*:/i,
                /onerror\s*=/i,
                /onload\s*=/i,
                /onclick\s*=/i,
                /onmouseover\s*=/i,
                /<iframe[\s\S]*?>/i,
                /<embed[\s\S]*?>/i,
                /<object[\s\S]*?>/i,
                /eval\s*\(/i,
                /expression\s*\(/i,
                /vbscript\s*:/i,
                /data\s*:\s*text\/html/i
            ];
            
            for (const pattern of xssPatterns) {
                if (pattern.test(address)) {
                    return { valid: false, message: 'Address contains potentially dangerous script patterns. Please remove script tags or event handlers' };
                }
            }
            
            return { valid: true, message: '' };
        },

        // Validate credit card number
        validateCreditCard: function (cardNumber) {
            if (!cardNumber) return { valid: false, message: 'Credit card number is required' };
            
            // Remove spaces and dashes
            cardNumber = cardNumber.replace(/[\s\-]/g, '');
            
            if (!/^\d{13,19}$/.test(cardNumber)) {
                return { valid: false, message: 'Credit card number must be between 13 and 19 digits' };
            }
            
            // Luhn algorithm for credit card validation
            if (!this.luhnCheck(cardNumber)) {
                return { valid: false, message: 'Please enter a valid credit card number' };
            }
            
            return { valid: true, message: '' };
        },

        // Luhn algorithm implementation
        luhnCheck: function (cardNumber) {
            let sum = 0;
            let isEven = false;
            
            for (let i = cardNumber.length - 1; i >= 0; i--) {
                let digit = parseInt(cardNumber.charAt(i), 10);
                
                if (isEven) {
                    digit *= 2;
                    if (digit > 9) {
                        digit -= 9;
                    }
                }
                
                sum += digit;
                isEven = !isEven;
            }
            
            return (sum % 10) === 0;
        },

        // Check for potential XSS attacks
        containsXss: function (input) {
            if (!input) return false;
            
            if (patterns.noHtml.test(input)) {
                return true;
            }
            
            return patterns.xssPatterns.some(pattern => pattern.test(input));
        },

        // Check for potential SQL injection
        containsSqlInjection: function (input) {
            if (!input) return false;
            
            return patterns.sqlPatterns.some(pattern => pattern.test(input));
        },

        // Sanitize input (remove dangerous characters)
        sanitize: function (input) {
            if (!input) return input;
            
            // Trim whitespace
            input = input.trim();
            
            // Remove null characters
            input = input.replace(/\0/g, '');
            
            // Remove control characters
            input = input.replace(/[\x00-\x08\x0B\x0C\x0E-\x1F\x7F]/g, '');
            
            return input;
        },

        // Show validation error
        showError: function (inputElement, message) {
            const feedbackElement = inputElement.nextElementSibling;
            
            if (feedbackElement && feedbackElement.classList.contains('text-danger')) {
                feedbackElement.textContent = message;
                feedbackElement.style.display = 'block';
            }
            
            inputElement.classList.add('is-invalid');
            inputElement.classList.remove('is-valid');
        },

        // Show validation success
        showSuccess: function (inputElement) {
            const feedbackElement = inputElement.nextElementSibling;
            
            if (feedbackElement && feedbackElement.classList.contains('text-danger')) {
                feedbackElement.textContent = '';
                feedbackElement.style.display = 'none';
            }
            
            inputElement.classList.add('is-valid');
            inputElement.classList.remove('is-invalid');
        },

        // Clear validation state
        clearValidation: function (inputElement) {
            const feedbackElement = inputElement.nextElementSibling;
            
            if (feedbackElement && feedbackElement.classList.contains('text-danger')) {
                feedbackElement.textContent = '';
                feedbackElement.style.display = 'none';
            }
            
            inputElement.classList.remove('is-invalid', 'is-valid');
        }
    };

    // Auto-format credit card number with spaces
    window.formatCreditCard = function (input) {
        let value = input.value.replace(/\s/g, '');
        let formattedValue = value.match(/.{1,4}/g)?.join(' ') || value;
        input.value = formattedValue;
    };

    // Auto-format mobile number with country code support
    window.formatMobile = function (input) {
        let value = input.value.replace(/\s/g, '');
        
        // Only allow digits and + at the start
        if (value.startsWith('+')) {
            // Keep the + and only digits after
            value = '+' + value.substring(1).replace(/\D/g, '');
        } else {
            // No country code - only digits
            value = value.replace(/\D/g, '');
        }
        
        let formattedValue = value;
        
        // Format based on country code
        if (value.startsWith('+65')) {
            // Singapore: +65 9759 3160
            const countryCode = value.substring(0, 3);
            const number = value.substring(3);
            if (number.length > 8) {
                formattedValue = countryCode + ' ' + number.substring(0, 4) + ' ' + number.substring(4, 8);
            } else if (number.length > 4) {
                formattedValue = countryCode + ' ' + number.substring(0, 4) + ' ' + number.substring(4);
            } else if (number.length > 0) {
                formattedValue = countryCode + ' ' + number;
            } else {
                formattedValue = countryCode;
            }
        } else if (value.startsWith('+60')) {
            // Malaysia: +60 12 345 6789
            const countryCode = value.substring(0, 3);
            const number = value.substring(3);
            if (number.length > 10) {
                formattedValue = countryCode + ' ' + number.substring(0, 2) + ' ' + number.substring(2, 5) + ' ' + number.substring(5, 9);
            } else if (number.length > 5) {
                formattedValue = countryCode + ' ' + number.substring(0, 2) + ' ' + number.substring(2, 5) + ' ' + number.substring(5);
            } else if (number.length > 2) {
                formattedValue = countryCode + ' ' + number.substring(0, 2) + ' ' + number.substring(2);
            } else if (number.length > 0) {
                formattedValue = countryCode + ' ' + number;
            } else {
                formattedValue = countryCode;
            }
        } else if (value.startsWith('+62')) {
            // Indonesia: +62 812 3456 7890
            const countryCode = value.substring(0, 3);
            const number = value.substring(3);
            if (number.length > 12) {
                formattedValue = countryCode + ' ' + number.substring(0, 3) + ' ' + number.substring(3, 7) + ' ' + number.substring(7, 11);
            } else if (number.length > 7) {
                formattedValue = countryCode + ' ' + number.substring(0, 3) + ' ' + number.substring(3, 7) + ' ' + number.substring(7);
            } else if (number.length > 3) {
                formattedValue = countryCode + ' ' + number.substring(0, 3) + ' ' + number.substring(3);
            } else if (number.length > 0) {
                formattedValue = countryCode + ' ' + number;
            } else {
                formattedValue = countryCode;
            }
        } else if (value.startsWith('+')) {
            // Other country codes - don't format, just limit length
            if (value.length > 15) {
                formattedValue = value.substring(0, 15);
            }
        } else {
            // No country code - Singapore format only (8 digits max)
            if (value.length > 8) {
                formattedValue = value.substring(0, 8);
            }
        }
        
        input.value = formattedValue;
    };

})();
