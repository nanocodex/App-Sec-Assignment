// Client-side input validation and sanitization
(function () {
    'use strict';

    // Validation patterns
    const patterns = {
        email: /^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$/,
        singaporeMobile: /^[89]\d{7}$/,
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
            
            return { valid: true, message: '' };
        },

        // Validate Singapore mobile number
        validateMobile: function (mobile) {
            if (!mobile) return { valid: false, message: 'Mobile number is required' };
            
            // Remove spaces and dashes
            mobile = mobile.replace(/[\s\-]/g, '');
            
            if (!patterns.singaporeMobile.test(mobile)) {
                return { valid: false, message: 'Mobile number must be 8 digits and start with 8 or 9' };
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

        // Validate address
        validateAddress: function (address, fieldName) {
            if (!address) return { valid: false, message: `${fieldName} is required` };
            
            address = address.trim();
            
            if (address.length < 5) {
                return { valid: false, message: 'Address is too short. Please enter a complete address' };
            }
            
            if (address.length > 200) {
                return { valid: false, message: 'Address cannot exceed 200 characters' };
            }
            
            if (!/^[a-zA-Z0-9\s.,\-#/()]+$/.test(address)) {
                return { valid: false, message: 'Address can only contain letters, numbers, spaces, and common punctuation (.,#-/)' };
            }
            
            if (this.containsXss(address)) {
                return { valid: false, message: 'Invalid characters detected in address' };
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

    // Auto-format mobile number
    window.formatMobile = function (input) {
        let value = input.value.replace(/\D/g, '');
        if (value.length > 8) {
            value = value.substring(0, 8);
        }
        input.value = value;
    };

})();
