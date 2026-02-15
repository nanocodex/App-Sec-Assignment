-- ============================================================================
-- QUICK FIX: HTML Encode Special Characters in Address Fields - ALL USERS
-- Applies HTML encoding to ALL users, regardless of current content
-- ============================================================================

USE [AspNetAuth]
GO

PRINT 'Encoding addresses for ALL users...'
PRINT ''

-- Display current state
SELECT TOP 10
    Email,
    Billing,
    Shipping
FROM AspNetUsers
ORDER BY Email

PRINT ''
PRINT 'Applying encoding to ALL users...'
PRINT ''

-- Update ALL records with HTML encoding
UPDATE AspNetUsers
SET 
    Billing = 
        REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(
        REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(
        REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(
            -- Protect existing &amp;
            REPLACE(Billing, '&amp;', '|||AMPERSAND|||'),
            '&', '&amp;'),
            '|||AMPERSAND|||', '&amp;'),
            '<', '&lt;'),
            '>', '&gt;'),
            '"', '&quot;'),
            '''', '&#x27;'),
            '$', '&#x24;'),
            '%', '&#x25;'),
            '^', '&#x5E;'),
            '*', '&#x2A;'),
            '(', '&#x28;'),
            ')', '&#x29;'),
            '{', '&#x7B;'),
            '}', '&#x7D;'),
            '[', '&#x5B;'),
            ']', '&#x5D;'),
            '+', '&#x2B;'),
            '=', '&#x3D;'),
            '!', '&#x21;'),
            '@', '&#x40;'),
            '#', '&#x23;'),
            '~', '&#x7E;'),
            '`', '&#x60;'),
            '|', '&#x7C;'),
            '/', '&#x2F;'),
    Shipping = 
        REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(
        REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(
        REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(
            -- Protect existing &amp;
            REPLACE(Shipping, '&amp;', '|||AMPERSAND|||'),
            '&', '&amp;'),
            '|||AMPERSAND|||', '&amp;'),
            '<', '&lt;'),
            '>', '&gt;'),
            '"', '&quot;'),
            '''', '&#x27;'),
            '$', '&#x24;'),
            '%', '&#x25;'),
            '^', '&#x5E;'),
            '*', '&#x2A;'),
            '(', '&#x28;'),
            ')', '&#x29;'),
            '{', '&#x7B;'),
            '}', '&#x7D;'),
            '[', '&#x5B;'),
            ']', '&#x5D;'),
            '+', '&#x2B;'),
            '=', '&#x3D;'),
            '!', '&#x21;'),
            '@', '&#x40;'),
            '#', '&#x23;'),
            '~', '&#x7E;'),
            '`', '&#x60;'),
            '|', '&#x7C;'),
            '/', '&#x2F;')
-- NO WHERE clause - applies to ALL users

PRINT 'Updated ' + CAST(@@ROWCOUNT AS VARCHAR) + ' records'
PRINT ''

-- Display results
SELECT TOP 10
    Email,
    Billing AS Billing_Encoded,
    Shipping AS Shipping_Encoded
FROM AspNetUsers
ORDER BY Email

PRINT ''
PRINT 'Complete! All users have been encoded.'
PRINT ''
PRINT 'To verify a specific user:'
PRINT 'SELECT Email, Billing, Shipping FROM AspNetUsers WHERE Email = ''your-email@example.com'''
GO
