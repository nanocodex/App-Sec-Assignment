# SQL Database Migration Guide: HTML Encode Address Fields

## Problem

The in-app migration tool (`DataMigrationService`) only encodes ampersands (`&`) but not other special characters like `$`, `%`, `^`, `*`, etc.

**Example Issue:**
- **Current in DB:** `$%^&amp;* &&^&amp; *&amp;*&amp;*&amp;`
- **Should be:** `&#x24;&#x25;&#x5E;&amp;&#x2A; &amp;&amp;&#x5E;&amp; &#x2A;&amp;&#x2A;&amp;&#x2A;&amp;`

## Solution

Use SQL scripts to directly update the database with proper HTML encoding for ALL special characters.

---

## ?? CRITICAL: Backup First!

**Before running any SQL script, create a backup:**

```sql
USE [master]
GO

BACKUP DATABASE [AspNetAuth] 
TO DISK = 'C:\Backups\AspNetAuth_BeforeAddressEncoding.bak'
WITH FORMAT, INIT, NAME = 'Full Backup Before Address Encoding Migration';
GO
```

**Verify backup:**
```sql
RESTORE VERIFYONLY 
FROM DISK = 'C:\Backups\AspNetAuth_BeforeAddressEncoding.bak'
GO
```

---

## Option 1: Quick Fix Script (Recommended)

**File:** `Database/QuickFixEncoding.sql`

**Use when:**
- You need a fast, simple solution
- You have a few records to update
- You want to run it manually and see immediate results

**How to execute:**

### Using SQL Server Management Studio (SSMS):
1. Open SSMS
2. Connect to `(localdb)\ProjectModels`
3. Open `Database/QuickFixEncoding.sql`
4. Click **Execute** (F5)

### Using Visual Studio:
1. Open **SQL Server Object Explorer**
2. Navigate to `(localdb)\ProjectModels` ? Databases ? AspNetAuth
3. Right-click ? **New Query**
4. Copy and paste contents of `Database/QuickFixEncoding.sql`
5. Click **Execute**

### Using Command Line:
```cmd
sqlcmd -S (localdb)\ProjectModels -d AspNetAuth -i "Database\QuickFixEncoding.sql"
```

**Expected Output:**
```
Fixing partially encoded addresses...

Email                    Billing                                  Shipping
------------------------ ---------------------------------------- ----------------------------------------
testuser@email.com      $%^&amp;*  &&^&amp;  *&amp;*&amp;*&amp;  $%^&amp;*  &&^&amp;  *&amp;*&amp;*&amp; 

Applying encoding...

Updated 1 records

Email                    Billing_Encoded                           Shipping_Encoded
------------------------ -----------------------------------------  -----------------------------------------
testuser@email.com      &#x24;&#x25;&#x5E;&amp;&#x2A;  ...       &#x24;&#x25;&#x5E;&amp;&#x2A;  ...

Complete!
```

---

## Option 2: Comprehensive Script

**File:** `Database/HtmlEncodeAddressFields.sql`

**Use when:**
- You want detailed before/after logging
- You need to see all changes made
- You want comprehensive verification queries

**How to execute:**
Same methods as Option 1, but use `HtmlEncodeAddressFields.sql` instead.

**Features:**
- ? Shows before and after values
- ? Counts records needing encoding
- ? Provides verification queries
- ? Detailed output log

---

## Option 3: Stored Procedure (Best for Production)

**File:** `Database/CreateEncodingStoredProcedure.sql`

**Use when:**
- You want a reusable solution
- You need to run encoding multiple times
- You want a professional, production-ready approach

**How to set up:**

### Step 1: Create the procedure
```sql
-- Run this once to create the stored procedure
-- Execute: Database/CreateEncodingStoredProcedure.sql
```

### Step 2: Execute the procedure
```sql
USE [AspNetAuth]
GO

EXEC [dbo].[HtmlEncodeAddressFields]
GO
```

### Step 3: View results
The procedure will automatically display:
- Total users in database
- Records needing encoding
- Before/after values for each updated record
- Completion summary

**Features:**
- ? Reusable - can run multiple times safely
- ? Idempotent - won't double-encode already encoded data
- ? Detailed logging with before/after comparison
- ? Returns count of updated records
- ? Professional stored procedure approach

---

## Character Encoding Reference

The scripts encode the following characters:

| Character | HTML Entity | Example |
|-----------|-------------|---------|
| `&` | `&amp;` | `AT&T` ? `AT&amp;T` |
| `<` | `&lt;` | `<tag>` ? `&lt;tag&gt;` |
| `>` | `&gt;` | `a>b` ? `a&gt;b` |
| `"` | `&quot;` | `"quote"` ? `&quot;quote&quot;` |
| `'` | `&#x27;` | `John's` ? `John&#x27;s` |
| `$` | `&#x24;` | `$100` ? `&#x24;100` |
| `%` | `&#x25;` | `50%` ? `50&#x25;` |
| `^` | `&#x5E;` | `2^3` ? `2&#x5E;3` |
| `*` | `&#x2A;` | `***` ? `&#x2A;&#x2A;&#x2A;` |
| `(` | `&#x28;` | `(test)` ? `&#x28;test&#x29;` |
| `)` | `&#x29;` | See above |
| `{` | `&#x7B;` | `{json}` ? `&#x7B;json&#x7D;` |
| `}` | `&#x7D;` | See above |
| `[` | `&#x5B;` | `[array]` ? `&#x5B;array&#x5D;` |
| `]` | `&#x5D;` | See above |
| `+` | `&#x2B;` | `C++` ? `C&#x2B;&#x2B;` |
| `=` | `&#x3D;` | `a=b` ? `a&#x3D;b` |
| `!` | `&#x21;` | `!important` ? `&#x21;important` |
| `@` | `&#x40;` | `@user` ? `&#x40;user` |
| `#` | `&#x23;` | `#tag` ? `&#x23;tag` |
| `~` | `&#x7E;` | `~test` ? `&#x7E;test` |
| `` ` `` | `&#x60;` | `` `code` `` ? `&#x60;code&#x60;` |
| `|` | `&#x7C;` | `a|b` ? `a&#x7C;b` |
| `\` | `&#x5C;` | `C:\path` ? `C:&#x5C;path` |
| `/` | `&#x2F;` | `a/b` ? `a&#x2F;b` |

---

## Special Handling for Ampersands

The scripts have special logic to avoid double-encoding ampersands that are already encoded as `&amp;`:

```sql
-- Step 1: Temporarily protect existing &amp;
REPLACE(Field, '&amp;', '|||AMPERSAND|||')

-- Step 2: Encode all & to &amp;
REPLACE(Field, '&', '&amp;')

-- Step 3: Restore protected &amp;
REPLACE(Field, '|||AMPERSAND|||', '&amp;')
```

This ensures:
- ? **Won't happen:** `&amp;` ? `&amp;amp;` (double encoding)
- ? **Will happen:** `&` ? `&amp;` (correct encoding)

---

## Verification Queries

After running the migration, verify the results:

### Check Specific User
```sql
SELECT 
    Email,
    Billing,
    Shipping
FROM AspNetUsers
WHERE Email = 'testuser@email.com'
```

### Count Encoded Records
```sql
SELECT COUNT(*) AS EncodedCount
FROM AspNetUsers
WHERE 
    Billing LIKE '%&#%' 
    OR Shipping LIKE '%&#%'
    OR Billing LIKE '%&amp;%'
    OR Shipping LIKE '%&amp;%'
```

### Find Records Still Needing Encoding
```sql
SELECT 
    Email,
    Billing,
    Shipping
FROM AspNetUsers
WHERE 
    (Billing LIKE '%[<>&"''$%^*(){}[\]+!=@#~`|\/]%' AND Billing NOT LIKE '%&#%')
    OR (Shipping LIKE '%[<>&"''$%^*(){}[\]+!=@#~`|\/]%' AND Shipping NOT LIKE '%&#%')
```

### View All Encoded Addresses
```sql
SELECT 
    Email,
    Billing AS 'Encoded Billing',
    Shipping AS 'Encoded Shipping'
FROM AspNetUsers
WHERE 
    Billing LIKE '%&#%' 
    OR Shipping LIKE '%&#%'
ORDER BY Email
```

---

## Test in Application

After running the SQL migration:

1. **Build the application:**
```bash
dotnet build
```

2. **Run the application:**
```bash
dotnet run
```

3. **Login** as `testuser@email.com`

4. **Navigate** to the home page

5. **Verify** addresses display correctly:
   - Should see: `$%^&* &&^& *&*&*&`
   - NOT: `&#x24;&#x25;&#x5E;&amp;&#x2A; ...`

---

## Troubleshooting

### Issue: Addresses show as HTML entities on the page

**Cause:** Missing `@Html.Raw()` in Razor view

**Solution:** Check `Pages/Index.cshtml`:
```razor
<!-- Correct -->
<div>@Html.Raw(Model.CurrentUser.Billing)</div>
<div>@Html.Raw(Model.CurrentUser.Shipping)</div>

<!-- Wrong -->
<div>@Model.CurrentUser.Billing</div>
<div>@Model.CurrentUser.Shipping</div>
```

### Issue: SQL script shows 0 records updated

**Cause:** Records already encoded OR no special characters present

**Solution:** Run verification query to check:
```sql
SELECT Email, Billing, Shipping
FROM AspNetUsers
WHERE Email = 'testuser@email.com'
```

If already encoded, you'll see `&#x` patterns in the data.

### Issue: Double encoding (e.g., `&amp;amp;`)

**Cause:** Running script multiple times without the ampersand protection logic

**Solution:** Use the provided scripts which have ampersand protection built-in. If already double-encoded, restore from backup.

### Issue: Can't execute stored procedure

**Cause:** Procedure not created or insufficient permissions

**Solution:**
1. Check if procedure exists:
```sql
SELECT * FROM sys.objects 
WHERE object_id = OBJECT_ID(N'[dbo].[HtmlEncodeAddressFields]') 
AND type in (N'P', N'PC')
```

2. If not found, run `CreateEncodingStoredProcedure.sql` first

3. Check permissions:
```sql
-- You need these permissions
GRANT EXECUTE ON [dbo].[HtmlEncodeAddressFields] TO [YourUser]
```

---

## Rollback Procedure

If something goes wrong, restore from backup:

```sql
USE [master]
GO

-- Set database to single user mode
ALTER DATABASE [AspNetAuth] SET SINGLE_USER WITH ROLLBACK IMMEDIATE
GO

-- Restore the backup
RESTORE DATABASE [AspNetAuth] 
FROM DISK = 'C:\Backups\AspNetAuth_BeforeAddressEncoding.bak'
WITH REPLACE
GO

-- Set back to multi-user mode
ALTER DATABASE [AspNetAuth] SET MULTI_USER
GO
```

---

## Best Practices

1. ? **Always backup before making changes**
2. ? **Test on a development database first**
3. ? **Run verification queries after migration**
4. ? **Test the application display after migration**
5. ? **Keep the backup for at least 30 days**
6. ? **Document the date and time of migration**
7. ? **Notify team members before running in production**

---

## Migration Checklist

- [ ] **Backup created** - `BACKUP DATABASE [AspNetAuth]`
- [ ] **Backup verified** - `RESTORE VERIFYONLY`
- [ ] **Chose migration method** - Quick Fix / Comprehensive / Stored Procedure
- [ ] **Script executed** - Ran SQL script in SSMS/VS
- [ ] **Results reviewed** - Checked output messages
- [ ] **Verification queries run** - Confirmed encoding successful
- [ ] **Application tested** - Addresses display correctly
- [ ] **No double encoding** - Verified no `&amp;amp;` patterns
- [ ] **Backup retained** - Saved backup file securely
- [ ] **Migration documented** - Recorded date/time/results

---

## Recommended Approach

For your specific case (`testuser@email.com` with partially encoded data):

### **Use Option 1: Quick Fix Script** ?

**Why:**
- Fast and simple
- Handles your exact scenario (already has `&amp;` but needs other chars encoded)
- Shows immediate results
- Easy to verify

**Steps:**
```bash
1. Backup database (critical!)
2. Open SSMS or VS SQL Server Object Explorer
3. Execute Database/QuickFixEncoding.sql
4. Verify testuser@email.com addresses are fully encoded
5. Test in application
```

**Time required:** 2-5 minutes

---

## Summary

| Method | Speed | Detail | Reusable | Best For |
|--------|-------|--------|----------|----------|
| **Quick Fix** | ? Fast | Basic | ? No | Immediate fixes |
| **Comprehensive** | ? Fast | High | ? No | One-time migration with logging |
| **Stored Procedure** | ? Fast | High | ? Yes | Production, repeated use |

**All methods are safe and idempotent - they won't double-encode already encoded data.**

---

## Support

If you encounter issues:
1. Check the troubleshooting section above
2. Run verification queries to diagnose
3. Review application logs
4. Restore from backup if needed

---

**You're ready to fix the encoding! Choose your method and follow the steps above.** ??
