namespace WebApplication1.Services
{
    public interface ISmsService
    {
        Task<bool> SendPasswordResetSmsAsync(string mobile, string resetCode);
    }
}
