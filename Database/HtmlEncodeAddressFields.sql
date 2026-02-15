-- ============================================================================
-- SQL Script: HTML Encode Billing and Shipping Address Fields
-- Database: AspNetAuth
-- Table: AspNetUsers
-- Purpose: Encode all special characters in Billing and Shipping fields for ALL users
-- ============================================================================

USE [AspNetAuth]
GO

-- Create a backup before running this script
-- BACKUP DATABASE [AspNetAuth] TO DISK = 'C:\Backups\AspNetAuth_BeforeEncoding.bak'

PRINT '============================================================================'
PRINT 'Starting HTML Encoding Migration for Address Fields - ALL USERS'
PRINT '============================================================================'
PRINT ''

-- Display current state
PRINT 'Current records (showing first 100):'
SELECT TOP 100
    Email,
    Billing AS Billing_Before,
    Shipping AS Shipping_Before
FROM AspNetUsers
ORDER BY Email
PRINT ''

-- Count total records to update
DECLARE @TotalUsers INT
SELECT @TotalUsers = COUNT(*) FROM AspNetUsers

PRINT 'Total users in database: ' + CAST(@TotalUsers AS VARCHAR)
PRINT ''

-- ============================================================================
-- STEP 1: Update Billing Field for ALL USERS
-- ============================================================================

PRINT 'Encoding Billing addresses for ALL users...'


UPDATE AspNetUsers
SET Billing = 
    -- Replace in order with ampersand protection
    REPLACE(
    REPLACE(
    REPLACE(
    REPLACE(
    REPLACE(
    REPLACE(
    REPLACE(
    REPLACE(
    REPLACE(
    REPLACE(
    REPLACE(
    REPLACE(
    REPLACE(
    REPLACE(
    REPLACE(
    REPLACE(
    REPLACE(
    REPLACE(
    REPLACE(
    REPLACE(
    REPLACE(
    REPLACE(
    REPLACE(
    REPLACE(
        -- First protect existing &amp; to avoid double-encoding
        REPLACE(Billing, '&amp;', '|||AMPERSAND|||'),
        -- Then encode all & characters
        '&', '&amp;'),
        -- Restore protected &amp;
        '|||AMPERSAND|||', '&amp;'),
        -- Encode other special characters
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
-- Apply to ALL users, no WHERE clause restrictions

PRINT 'Billing addresses encoded: ' + CAST(@@ROWCOUNT AS VARCHAR) + ' records'
PRINT ''

-- ============================================================================
-- STEP 2: Update Shipping Field for ALL USERS
-- ============================================================================

PRINT 'Encoding Shipping addresses for ALL users...'


UPDATE AspNetUsers
SET Shipping = 
    -- Replace in order with ampersand protection
    REPLACE(
    REPLACE(
    REPLACE(
    REPLACE(
    REPLACE(
    REPLACE(
    REPLACE(
    REPLACE(
    REPLACE(
    REPLACE(
    REPLACE(
    REPLACE(
    REPLACE(
    REPLACE(
    REPLACE(
    REPLACE(
    REPLACE(
    REPLACE(
    REPLACE(
    REPLACE(
    REPLACE(
    REPLACE(
    REPLACE(
    REPLACE(
        -- First protect existing &amp; to avoid double-encoding
        REPLACE(Shipping, '&amp;', '|||AMPERSAND|||'),
        -- Then encode all & characters
        '&', '&amp;'),
        -- Restore protected &amp;
        '|||AMPERSAND|||', '&amp;'),
        -- Encode other special characters
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
-- Apply to ALL users, no WHERE clause restrictions

PRINT 'Shipping addresses encoded: ' + CAST(@@ROWCOUNT AS VARCHAR) + ' records'
PRINT ''

-- ============================================================================
-- STEP 3: Display Results
-- ============================================================================

PRINT '============================================================================'
PRINT 'Migration Complete - Results (showing first 100):'
PRINT '============================================================================'
PRINT ''

SELECT TOP 100
    Email,
    Billing AS Billing_After,
    Shipping AS Shipping_After
FROM AspNetUsers
ORDER BY Email

PRINT ''
PRINT '============================================================================'
PRINT 'Verification Queries:'
PRINT '============================================================================'
PRINT ''
PRINT '-- Check specific user:'
PRINT 'SELECT Email, Billing, Shipping FROM AspNetUsers WHERE Email = ''testuser@email.com'''
PRINT ''
PRINT '-- Count all users:'
PRINT 'SELECT COUNT(*) AS TotalUsers FROM AspNetUsers'
PRINT ''
PRINT '-- View all encoded addresses:'
PRINT 'SELECT Email, Billing, Shipping FROM AspNetUsers ORDER BY Email'
PRINT ''
PRINT '============================================================================'
GO
