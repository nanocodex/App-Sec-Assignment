namespace WebApplication1.Services
{
    public interface IEmailService
    {
        Task<bool> SendPasswordResetEmailAsync(string email, string resetLink);
        Task<bool> SendPasswordChangedNotificationAsync(string email, string userName);
    }
}
