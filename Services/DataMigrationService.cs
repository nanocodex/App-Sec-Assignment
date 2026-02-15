using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Text.Encodings.Web;
using WebApplication1.Model;

namespace WebApplication1.Services
{
    public class DataMigrationService : IDataMigrationService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly HtmlEncoder _htmlEncoder;
        private readonly ILogger<DataMigrationService> _logger;

        public DataMigrationService(
            UserManager<ApplicationUser> userManager,
            HtmlEncoder htmlEncoder,
            ILogger<DataMigrationService> logger)
        {
            _userManager = userManager;
            _htmlEncoder = htmlEncoder;
            _logger = logger;
        }

        public async Task<DataMigrationResult> EncodeExistingAddressFieldsAsync()
        {
            var result = new DataMigrationResult { Success = true };

            try
            {
                _logger.LogInformation("Starting address field encoding migration...");

                // Get all users
                var users = await _userManager.Users.ToListAsync();
                result.RecordsProcessed = users.Count;

                _logger.LogInformation("Found {Count} users to process", users.Count);

                foreach (var user in users)
                {
                    try
                    {
                        bool needsUpdate = false;
                        var originalBilling = user.Billing;
                        var originalShipping = user.Shipping;

                        // Check if Billing needs encoding (contains special characters not yet encoded)
                        if (!string.IsNullOrEmpty(user.Billing) && NeedsEncoding(user.Billing))
                        {
                            user.Billing = _htmlEncoder.Encode(user.Billing);
                            needsUpdate = true;
                            _logger.LogInformation("Encoding Billing for user {Email}: '{Original}' -> '{Encoded}'", 
                                user.Email, originalBilling, user.Billing);
                        }

                        // Check if Shipping needs encoding
                        if (!string.IsNullOrEmpty(user.Shipping) && NeedsEncoding(user.Shipping))
                        {
                            user.Shipping = _htmlEncoder.Encode(user.Shipping);
                            needsUpdate = true;
                            _logger.LogInformation("Encoding Shipping for user {Email}: '{Original}' -> '{Encoded}'", 
                                user.Email, originalShipping, user.Shipping);
                        }

                        // Update the user if changes were made
                        if (needsUpdate)
                        {
                            var updateResult = await _userManager.UpdateAsync(user);
                            if (updateResult.Succeeded)
                            {
                                result.RecordsUpdated++;
                                result.Messages.Add($"? Encoded addresses for user: {user.Email}");
                            }
                            else
                            {
                                result.RecordsFailed++;
                                var errors = string.Join(", ", updateResult.Errors.Select(e => e.Description));
                                result.Errors.Add($"? Failed to update user {user.Email}: {errors}");
                                _logger.LogError("Failed to update user {Email}: {Errors}", user.Email, errors);
                            }
                        }
                        else
                        {
                            result.Messages.Add($"- User {user.Email} already has encoded addresses or no special characters");
                        }
                    }
                    catch (Exception ex)
                    {
                        result.RecordsFailed++;
                        result.Errors.Add($"? Error processing user {user.Email}: {ex.Message}");
                        _logger.LogError(ex, "Error processing user {Email}", user.Email);
                    }
                }

                result.Success = result.RecordsFailed == 0;
                
                _logger.LogInformation(
                    "Address encoding migration completed. Processed: {Processed}, Updated: {Updated}, Failed: {Failed}",
                    result.RecordsProcessed, result.RecordsUpdated, result.RecordsFailed);

                return result;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Errors.Add($"Critical error during migration: {ex.Message}");
                _logger.LogError(ex, "Critical error during address encoding migration");
                return result;
            }
        }

        /// <summary>
        /// Checks if a string contains special characters that need HTML encoding
        /// </summary>
        private bool NeedsEncoding(string input)
        {
            if (string.IsNullOrEmpty(input))
                return false;

            // Check for common special characters that would be encoded
            var specialChars = new[] { '<', '>', '&', '"', '\'', '$', '%', '^', '*', '(', ')', '{', '}', '[', ']' };
            
            // If the string contains these characters but doesn't have encoded entities, it needs encoding
            if (specialChars.Any(c => input.Contains(c)))
            {
                // Check if already encoded by looking for HTML entities
                // If it contains &lt; &gt; &amp; etc., it's already encoded
                if (input.Contains("&lt;") || input.Contains("&gt;") || input.Contains("&amp;") || 
                    input.Contains("&quot;") || input.Contains("&#"))
                {
                    return false; // Already encoded
                }
                
                return true; // Has special chars but not encoded
            }

            return false; // No special characters
        }
    }
}
