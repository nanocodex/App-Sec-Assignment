# Quick Start: Running Address Encoding Migration

## Prerequisites
- ? You must be logged in to the application
- ? **IMPORTANT:** Backup your database before proceeding

## Step-by-Step Instructions

### Step 1: Backup Your Database

**Option A: Using SQL Server Management Studio (SSMS)**
1. Right-click on `AspNetAuth` database
2. Select Tasks > Back Up...
3. Choose backup destination
4. Click OK

**Option B: Using SQL Command**
```sql
BACKUP DATABASE [AspNetAuth] 
TO DISK = 'C:\Backups\AspNetAuth_BeforeMigration.bak'
WITH FORMAT, INIT, NAME = 'Full Backup Before Address Encoding';
```

### Step 2: Access the Migration Tool

1. **Login** to your application
2. Click on **"Security"** dropdown in the navigation bar
3. Select **"Data Migration"**

### Step 3: Review the Information

On the Data Migration page, you'll see:
- ?? Explanation of what the migration does
- ?? Important warnings
- ?? Example transformations

**Read everything carefully before proceeding.**

### Step 4: Run the Migration

1. Click the **"Run Address Encoding Migration"** button (orange/warning colored)
2. You'll see a confirmation dialog:
   ```
   Are you sure you want to encode all address fields? 
   This will modify database records. 
   Make sure you have a backup!
   ```
3. Click **OK** to proceed

### Step 5: Wait for Completion

The migration will process all users. You'll see:
- ?? Processing message
- ?? Progress updates (in logs)

**Time:** Usually takes a few seconds for small databases, up to a minute for larger ones.

### Step 6: Review Results

After completion, you'll see:

**Statistics Cards:**
- **Total Records**: Number of users processed
- **Successfully Updated**: Users whose addresses were encoded
- **Failed**: Any records that failed (should be 0)

**Processing Details:**
- ? Green checkmarks for successfully updated users
- - Gray text for users already encoded or no special characters

**Errors (if any):**
- ? Red text showing any errors that occurred

### Step 7: Verify the Results

**Check the Database:**
```sql
-- View encoded addresses
SELECT 
    Email,
    Billing,
    Shipping
FROM AspNetUsers
WHERE Email = 'testuser@email.com';
```

**Check the Application:**
1. Go to the **Home** page
2. View your profile
3. Verify Billing and Shipping addresses display correctly

**Expected:**
- Addresses should look normal to the user
- Special characters should display as they were entered
- No visible difference in user experience

## Example Results

### Before Migration
```
Email: testuser@email.com
Billing: 123 Main St #05-67
Shipping: $%^&* &&^& *&*&*&
```

### After Migration (in Database)
```
Email: testuser@email.com
Billing: 123 Main St &#x23;05-67
Shipping: &#x24;&#x25;&#x5E;&amp;&#x2A; &amp;&amp;&#x5E;&amp; &#x2A;&amp;&#x2A;&amp;&#x2A;&amp;
```

### After Migration (on Screen)
```
Email: testuser@email.com
Billing: 123 Main St #05-67
Shipping: $%^&* &&^& *&*&*&
```

**Note:** The display looks the same, but it's now safe from XSS attacks!

## Common Questions

### Q: Can I run this multiple times?
**A:** Yes! The migration is idempotent. If you run it again:
- Already-encoded records will be skipped
- Only new or unencoded records will be updated
- No risk of double-encoding

### Q: What if I see errors?
**A:** Check the error log in the results section. Common issues:
- User record has invalid data
- Database permission issues
- Restore from backup and contact support

### Q: Will this affect how users see their data?
**A:** No. Users will see the exact same text. The encoding is transparent because:
- Razor automatically decodes HTML entities when displaying
- The browser renders the original characters
- Only the database storage format changes

### Q: What characters are encoded?
**A:** Special characters including:
- `< > & " '` (HTML special chars)
- `$ % ^ * ( ) { } [ ]` (potentially dangerous chars)

### Q: How do I undo this?
**A:** Restore from the backup you created in Step 1:
```sql
RESTORE DATABASE [AspNetAuth] 
FROM DISK = 'C:\Backups\AspNetAuth_BeforeMigration.bak'
WITH REPLACE;
```

## Troubleshooting

### Issue: Migration page is not accessible
**Solution:** Make sure you're logged in. The page requires authentication.

### Issue: All records show "already encoded"
**Solution:** The migration has already run. This is normal and safe.

### Issue: Migration fails immediately
**Solution:** 
1. Check application logs
2. Verify database connection
3. Ensure you have permission to update user records

### Issue: Some records fail to update
**Solution:**
1. Review the error log on the results page
2. Check application logs for detailed error messages
3. Fix the specific user records causing issues
4. Re-run the migration

## Success Indicators

? **Migration Successful If:**
- "Successfully Updated" count matches expected users with special characters
- "Failed" count is 0
- No errors in the error log
- Addresses display correctly on the home page
- Database shows encoded values (with `&#x` entities)

## Post-Migration Checklist

- [ ] Migration completed successfully
- [ ] Results reviewed and verified
- [ ] Database checked for encoded values
- [ ] Application tested with user profiles
- [ ] Backup retained for rollback if needed
- [ ] Migration documented in change log

## Need Help?

If you encounter issues:
1. Check the error messages in the migration results
2. Review application logs (`Logging` section in appsettings.json)
3. Check database logs for SQL errors
4. Restore from backup if necessary
5. Contact technical support with error details

## Summary

**What This Does:**
- ?? Secures existing address data against XSS attacks
- ?? Converts special characters to HTML entities
- ?? Logs all changes for audit purposes
- ? Safe to run multiple times

**What This Doesn't Do:**
- ? Doesn't change how data looks to users
- ? Doesn't delete or remove any data
- ? Doesn't require users to re-enter addresses
- ? Doesn't affect new registrations (they're already secure)

**Time Required:** 1-5 minutes depending on database size

**Risk Level:** Low (if backup is created first)

---

**Ready to proceed?** Follow the steps above and your database will be secured! ??
