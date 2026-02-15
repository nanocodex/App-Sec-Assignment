using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using WebApplication1.Model;

namespace WebApplication1.Services
{
    public class PasswordManagementService : IPasswordManagementService
    {
        private readonly AuthDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _configuration;
        private readonly ILogger<PasswordManagementService> _logger;

        // Configuration values
        private readonly int _passwordHistoryCount;
        private readonly int _minPasswordAgeMinutes;
        private readonly int _maxPasswordAgeDays;

        public PasswordManagementService(
            AuthDbContext context,
            UserManager<ApplicationUser> userManager,
            IConfiguration configuration,
            ILogger<PasswordManagementService> logger)
        {
            _context = context;
            _userManager = userManager;
            _configuration = configuration;
            _logger = logger;

            // Load configuration with defaults
            _passwordHistoryCount = _configuration.GetValue<int>("PasswordPolicy:HistoryCount", 2);
            _minPasswordAgeMinutes = _configuration.GetValue<int>("PasswordPolicy:MinPasswordAgeMinutes", 5);
            _maxPasswordAgeDays = _configuration.GetValue<int>("PasswordPolicy:MaxPasswordAgeDays", 90);
        }

        public async Task<bool> CheckPasswordHistoryAsync(string userId, string newPassword)
        {
            try
            {
                // Get the last N password hashes for the user
                var passwordHistories = await _context.PasswordHistories
                    .Where(ph => ph.UserId == userId)
                    .OrderByDescending(ph => ph.CreatedAt)
                    .Take(_passwordHistoryCount)
                    .ToListAsync();

                // Check if the new password matches any of the previous passwords
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null) return true;

                foreach (var history in passwordHistories)
                {
                    var result = _userManager.PasswordHasher.VerifyHashedPassword(
                        user, 
                        history.PasswordHash, 
                        newPassword);

                    if (result == PasswordVerificationResult.Success)
                    {
                        _logger.LogWarning("User {UserId} attempted to reuse a previous password", userId);
                        return false;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking password history for user {UserId}", userId);
                return true; // Allow password change on error to prevent lockout
            }
        }

        public async Task AddPasswordToHistoryAsync(string userId, string passwordHash)
        {
            try
            {
                var passwordHistory = new PasswordHistory
                {
                    UserId = userId,
                    PasswordHash = passwordHash,
                    CreatedAt = DateTime.UtcNow
                };

                _context.PasswordHistories.Add(passwordHistory);
                await _context.SaveChangesAsync();

                // Clean up old password history (keep only the configured number)
                var oldHistories = await _context.PasswordHistories
                    .Where(ph => ph.UserId == userId)
                    .OrderByDescending(ph => ph.CreatedAt)
                    .Skip(_passwordHistoryCount)
                    .ToListAsync();

                if (oldHistories.Any())
                {
                    _context.PasswordHistories.RemoveRange(oldHistories);
                    await _context.SaveChangesAsync();
                }

                _logger.LogInformation("Password history added for user {UserId}", userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding password to history for user {UserId}", userId);
            }
        }

        public async Task<(bool CanChange, string Message)> CanChangePasswordAsync(string userId)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return (false, "User not found.");
                }

                // Check if user must change password (override minimum age)
                if (user.MustChangePassword)
                {
                    return (true, string.Empty);
                }

                // Check minimum password age
                if (user.LastPasswordChangeDate.HasValue)
                {
                    var timeSinceLastChange = DateTime.UtcNow - user.LastPasswordChangeDate.Value;
                    var minAge = TimeSpan.FromMinutes(_minPasswordAgeMinutes);

                    if (timeSinceLastChange < minAge)
                    {
                        var remainingTime = minAge - timeSinceLastChange;
                        var message = $"You must wait {Math.Ceiling(remainingTime.TotalMinutes)} more minute(s) before changing your password again.";
                        _logger.LogWarning("User {UserId} attempted to change password before minimum age", userId);
                        return (false, message);
                    }
                }

                return (true, string.Empty);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if user {UserId} can change password", userId);
                return (false, "An error occurred. Please try again.");
            }
        }

        public async Task<bool> MustChangePasswordAsync(string userId)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null) return false;

                // Check if forced password change
                if (user.MustChangePassword) return true;

                // Check maximum password age
                if (user.LastPasswordChangeDate.HasValue && user.PasswordExpiryDate.HasValue)
                {
                    return DateTime.UtcNow >= user.PasswordExpiryDate.Value;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if user {UserId} must change password", userId);
                return false;
            }
        }

        public async Task UpdatePasswordChangeDateAsync(string userId)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null) return;

                user.LastPasswordChangeDate = DateTime.UtcNow;
                user.PasswordExpiryDate = DateTime.UtcNow.AddDays(_maxPasswordAgeDays);
                user.MustChangePassword = false;

                await _userManager.UpdateAsync(user);
                _logger.LogInformation("Password change date updated for user {UserId}", userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating password change date for user {UserId}", userId);
            }
        }

        public async Task<(bool Success, string Token)> GeneratePasswordResetTokenAsync(string userId)
        {
            try
            {
                // Generate a cryptographically secure token
                var tokenBytes = new byte[32];
                using (var rng = RandomNumberGenerator.Create())
                {
                    rng.GetBytes(tokenBytes);
                }
                var token = Convert.ToBase64String(tokenBytes)
                    .Replace("+", "-")
                    .Replace("/", "_")
                    .Replace("=", "");

                // Store the token in database
                var resetToken = new PasswordResetToken
                {
                    UserId = userId,
                    Token = token,
                    CreatedAt = DateTime.UtcNow,
                    ExpiresAt = DateTime.UtcNow.AddHours(1),
                    IsUsed = false
                };

                _context.PasswordResetTokens.Add(resetToken);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Password reset token generated for user {UserId}", userId);
                return (true, token);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating password reset token for user {UserId}", userId);
                return (false, string.Empty);
            }
        }

        public async Task<bool> ValidatePasswordResetTokenAsync(string userId, string token)
        {
            try
            {
                var resetToken = await _context.PasswordResetTokens
                    .Where(t => t.UserId == userId && t.Token == token && !t.IsUsed)
                    .OrderByDescending(t => t.CreatedAt)
                    .FirstOrDefaultAsync();

                if (resetToken == null)
                {
                    _logger.LogWarning("Invalid or already used password reset token for user {UserId}", userId);
                    return false;
                }

                if (DateTime.UtcNow > resetToken.ExpiresAt)
                {
                    _logger.LogWarning("Expired password reset token for user {UserId}", userId);
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating password reset token for user {UserId}", userId);
                return false;
            }
        }

        public async Task<bool> UsePasswordResetTokenAsync(string userId, string token)
        {
            try
            {
                var resetToken = await _context.PasswordResetTokens
                    .Where(t => t.UserId == userId && t.Token == token && !t.IsUsed)
                    .FirstOrDefaultAsync();

                if (resetToken == null || DateTime.UtcNow > resetToken.ExpiresAt)
                {
                    return false;
                }

                resetToken.IsUsed = true;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Password reset token used for user {UserId}", userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error using password reset token for user {UserId}", userId);
                return false;
            }
        }

        public string GenerateSmsResetCode()
        {
            // Generate a 6-digit code
            var random = new Random();
            return random.Next(100000, 999999).ToString();
        }
    }
}
