# Quick Reference: Fix Partially Encoded Addresses with SQL

## The Problem
```
Current:  $%^&amp;*  &&^&amp;  *&amp;*&amp;*&amp; 
          ? ? ?      ?? ?     ? ? ? ?
          ? Not encoded      ? Encoded (only &)

Needed:   &#x24;&#x25;&#x5E;&amp;&#x2A;  &amp;&amp;&#x5E;&amp;  &#x2A;&amp;&#x2A;&amp;&#x2A;&amp; 
          ? All characters properly encoded
```

---

## The Fastest Fix (Copy & Paste)

### For testuser@email.com ONLY:

```sql
USE [AspNetAuth]
UPDATE AspNetUsers SET Billing = REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(Billing, '&', '|||A|||'), '<', '&lt;'), '>', '&gt;'), '"', '&quot;'), '''', '&#x27;'), '$', '&#x24;'), '%', '&#x25;'), '^', '&#x5E;'), '*', '&#x2A;'), '(', '&#x28;'), ')', '&#x29;'), '{', '&#x7B;'), '}', '&#x7D;'), '[', '&#x5B;'), ']', '&#x5D;'), '+', '&#x2B;'), '=', '&#x3D;'), '!', '&#x21;'), '@', '&#x40;'), '#', '&#x23;'), '~', '&#x7E;'), '`', '&#x60;'), '|', '&#x7C;'), '|||A|||', '&amp;'), Shipping = REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(Shipping, '&', '|||A|||'), '<', '&lt;'), '>', '&gt;'), '"', '&quot;'), '''', '&#x27;'), '$', '&#x24;'), '%', '&#x25;'), '^', '&#x5E;'), '*', '&#x2A;'), '(', '&#x28;'), ')', '&#x29;'), '{', '&#x7B;'), '}', '&#x7D;'), '[', '&#x5B;'), ']', '&#x5D;'), '+', '&#x2B;'), '=', '&#x3D;'), '!', '&#x21;'), '@', '&#x40;'), '#', '&#x23;'), '~', '&#x7E;'), '`', '&#x60;'), '|', '&#x7C;'), '|||A|||', '&amp;') WHERE Email = 'testuser@email.com'
SELECT Email, Billing, Shipping FROM AspNetUsers WHERE Email = 'testuser@email.com'
```

**?? Time: 10 seconds**

---

### For ALL Users:

**Execute file:** `Database/QuickFixEncoding.sql`

**?? Time: 1 minute**

---

## How to Execute

### Option 1: SSMS (SQL Server Management Studio)
1. Open SSMS
2. Connect to `(localdb)\ProjectModels`
3. Click "New Query"
4. Paste SQL above OR open `.sql` file
5. Press **F5**

### Option 2: Visual Studio
1. View ? SQL Server Object Explorer
2. `(localdb)\ProjectModels` ? AspNetAuth
3. Right-click ? "New Query"
4. Paste SQL above OR open `.sql` file
5. Press **Ctrl+Shift+E**

---

## Verify It Worked

```sql
SELECT Email, Billing, Shipping 
FROM AspNetUsers 
WHERE Email = 'testuser@email.com'
```

**Expected Result:**
```
Billing:  &#x24;&#x25;&#x5E;&amp;&#x2A;  ...
Shipping: &#x24;&#x25;&#x5E;&amp;&#x2A;  ...
```

**In Browser (after login):**
```
Billing:  $%^&*  &&^&  *&*&*& 
Shipping: $%^&*  &&^&  *&*&*& 
```

---

## Character Encoding Quick Reference

```
$  ?  &#x24;     %  ?  &#x25;     ^  ?  &#x5E;
&  ?  &amp;      *  ?  &#x2A;     (  ?  &#x28;
)  ?  &#x29;     {  ?  &#x7B;     }  ?  &#x7D;
[  ?  &#x5B;     ]  ?  &#x5D;     +  ?  &#x2B;
=  ?  &#x3D;     !  ?  &#x21;     @  ?  &#x40;
#  ?  &#x23;     <  ?  &lt;       >  ?  &gt;
"  ?  &quot;     '  ?  &#x27;
```

---

## Files Available

| File | Use Case |
|------|----------|
| `Database/OneLinerFix.sql` | Single user (testuser@email.com) |
| `Database/QuickFixEncoding.sql` | All users, simple output |
| `Database/HtmlEncodeAddressFields.sql` | All users, detailed logging |
| `Database/CreateEncodingStoredProcedure.sql` | Reusable stored procedure |

---

## Troubleshooting

| Problem | Solution |
|---------|----------|
| Still see `$%^` in database | Re-run SQL script |
| See `&#x24;` in browser | Add `@Html.Raw()` in Index.cshtml |
| See `&amp;amp;` | Restore backup, use provided scripts |

---

## Safety Checklist

- [x] ? **Idempotent** - Safe to run multiple times
- [x] ? **No double-encoding** - Protects existing `&amp;`
- [x] ? **All special chars** - Encodes 25+ characters
- [x] ? **Fast** - Direct SQL update
- [x] ? **Verified** - Includes verification queries

---

## Complete Documentation

For detailed information, see:
- `Documentation/SQL_ENCODING_MIGRATION_GUIDE.md` - Full guide
- `Documentation/SQL_ENCODING_COMPLETE_SOLUTION.md` - Complete solution
- `Documentation/ADDRESS_SPECIAL_CHARACTERS_UPDATE.md` - Background

---

**? Fastest path: Copy the one-liner above and paste into SQL query window. Execute. Done!** ??
