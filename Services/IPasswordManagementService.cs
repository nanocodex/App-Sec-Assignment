namespace WebApplication1.Services
{
    public interface IPasswordManagementService
    {
        Task<bool> CheckPasswordHistoryAsync(string userId, string newPassword);
        Task AddPasswordToHistoryAsync(string userId, string passwordHash);
        Task<(bool CanChange, string Message)> CanChangePasswordAsync(string userId);
        Task<bool> MustChangePasswordAsync(string userId);
        Task UpdatePasswordChangeDateAsync(string userId);
        Task<(bool Success, string Token)> GeneratePasswordResetTokenAsync(string userId);
        Task<bool> ValidatePasswordResetTokenAsync(string userId, string token);
        Task<bool> UsePasswordResetTokenAsync(string userId, string token);
        string GenerateSmsResetCode();
    }
}
