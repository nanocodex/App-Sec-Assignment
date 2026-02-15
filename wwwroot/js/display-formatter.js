// Display formatting utilities for sensitive data
(function () {
    'use strict';

    window.DisplayFormatter = {
        /**
         * Format phone number for display based on country code
         * @param {string} phone - Phone number (with or without formatting)
         * @returns {string} Formatted phone number
         */
        formatPhoneDisplay: function(phone) {
            if (!phone) return '';
            
            // Remove all spaces first
            const cleanPhone = phone.replace(/\s/g, '');
            
            // Check for country code
            if (cleanPhone.startsWith('+65')) {
                // Singapore: +65 9759 3160
                const countryCode = cleanPhone.substring(0, 3);
                const number = cleanPhone.substring(3);
                if (number.length === 8) {
                    return `${countryCode} ${number.substring(0, 4)} ${number.substring(4)}`;
                }
                return cleanPhone;
            }
            else if (cleanPhone.startsWith('+60')) {
                // Malaysia: +60 12 345 6789
                const countryCode = cleanPhone.substring(0, 3);
                const number = cleanPhone.substring(3);
                if (number.length >= 9) {
                    return `${countryCode} ${number.substring(0, 2)} ${number.substring(2, 5)} ${number.substring(5)}`;
                }
                return cleanPhone;
            }
            else if (cleanPhone.startsWith('+62')) {
                // Indonesia: +62 812 3456 7890
                const countryCode = cleanPhone.substring(0, 3);
                const number = cleanPhone.substring(3);
                if (number.length >= 9) {
                    return `${countryCode} ${number.substring(0, 3)} ${number.substring(3, 7)} ${number.substring(7)}`;
                }
                return cleanPhone;
            }
            else if (cleanPhone.startsWith('+')) {
                // Other country codes - return as-is
                return cleanPhone;
            }
            else {
                // No country code - Singapore local format (no spacing)
                return cleanPhone;
            }
        },

        /**
         * Format credit card number for display
         * @param {string} cardNumber - Credit card number (with or without formatting)
         * @returns {string} Formatted credit card number (XXXX XXXX XXXX XXXX)
         */
        formatCreditCardDisplay: function(cardNumber) {
            if (!cardNumber) return '';
            
            // Remove all spaces
            const cleanCard = cardNumber.replace(/\s/g, '');
            
            // Format in groups of 4
            const formatted = cleanCard.match(/.{1,4}/g);
            return formatted ? formatted.join(' ') : cleanCard;
        },

        /**
         * Mask credit card number (show last 4 digits only)
         * @param {string} cardNumber - Credit card number
         * @returns {string} Masked card number (****-****-****-1234)
         */
        maskCreditCard: function(cardNumber) {
            if (!cardNumber) return '';
            
            const cleanCard = cardNumber.replace(/\s/g, '');
            if (cleanCard.length >= 4) {
                const last4 = cleanCard.substring(cleanCard.length - 4);
                return `****-****-****-${last4}`;
            }
            return '****';
        }
    };

})();
