-- ============================================================================
-- Stored Procedure: HTML Encode Address Fields for ALL USERS
-- Purpose: Reusable procedure to encode Billing and Shipping addresses
-- ============================================================================

USE [AspNetAuth]
GO

-- Drop existing procedure if it exists
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[HtmlEncodeAddressFields]') AND type in (N'P', N'PC'))
DROP PROCEDURE [dbo].[HtmlEncodeAddressFields]
GO

CREATE PROCEDURE [dbo].[HtmlEncodeAddressFields]
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @UpdatedCount INT = 0
    DECLARE @TotalRecords INT = 0
    
    -- Count total users
    SELECT @TotalRecords = COUNT(*) FROM AspNetUsers
    
    PRINT '============================================================================'
    PRINT 'HTML Encoding Address Fields - ALL USERS - Started at ' + CONVERT(VARCHAR, GETDATE(), 120)
    PRINT '============================================================================'
    PRINT 'Total users in database: ' + CAST(@TotalRecords AS VARCHAR)
    PRINT 'Applying encoding to ALL users...'
    PRINT ''
    
    -- Create a table variable to store user details before update
    DECLARE @UpdateLog TABLE (
        Email NVARCHAR(256),
        BillingBefore NVARCHAR(MAX),
        ShippingBefore NVARCHAR(MAX),
        BillingAfter NVARCHAR(MAX),
        ShippingAfter NVARCHAR(MAX)
    )
    
    -- Store before values for ALL users
    INSERT INTO @UpdateLog (Email, BillingBefore, ShippingBefore)
    SELECT Email, Billing, Shipping
    FROM AspNetUsers
    
    PRINT 'Records to be encoded: ' + CAST(@@ROWCOUNT AS VARCHAR)
    PRINT ''
    
    -- Update Billing and Shipping fields with HTML encoding for ALL USERS
    UPDATE AspNetUsers
    SET 
        Billing = 
            REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(
            REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(
            REPLACE(REPLACE(REPLACE(REPLACE(
                -- First, temporarily replace existing &amp; to protect it
                REPLACE(Billing, '&amp;', '|||AMPERSAND|||'),
                -- Then encode & 
                '&', '&amp;'),
                -- Restore protected &amp;
                '|||AMPERSAND|||', '&amp;'),
                -- Encode other characters
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
            REPLACE(REPLACE(REPLACE(REPLACE(
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
    
    SET @UpdatedCount = @@ROWCOUNT
    
    -- Store after values
    UPDATE ul
    SET 
        BillingAfter = u.Billing,
        ShippingAfter = u.Shipping
    FROM @UpdateLog ul
    INNER JOIN AspNetUsers u ON ul.Email = u.Email
    
    -- Display results (limit to first 100 for readability)
    PRINT ''
    PRINT 'Encoding complete. Updated records: ' + CAST(@UpdatedCount AS VARCHAR)
    PRINT ''
    PRINT 'Sample of changes (first 100 records):'
    PRINT '============================================================================'
    
    SELECT TOP 100
        Email,
        BillingBefore AS 'Billing Before',
        BillingAfter AS 'Billing After',
        ShippingBefore AS 'Shipping Before',
        ShippingAfter AS 'Shipping After'
    FROM @UpdateLog
    ORDER BY Email
    
    PRINT ''
    PRINT '============================================================================'
    PRINT 'Completed at ' + CONVERT(VARCHAR, GETDATE(), 120)
    PRINT 'All ' + CAST(@TotalRecords AS VARCHAR) + ' user records have been encoded.'
    PRINT '============================================================================'
    
    RETURN @UpdatedCount
END
GO

PRINT 'Stored procedure created successfully!'
PRINT ''
PRINT 'To execute and encode ALL users, run: EXEC [dbo].[HtmlEncodeAddressFields]'
PRINT ''
GO
