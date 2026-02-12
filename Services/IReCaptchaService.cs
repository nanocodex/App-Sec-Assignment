namespace WebApplication1.Services
{
    public interface IReCaptchaService
    {
        Task<bool> VerifyTokenAsync(string token, string action);
    }
}
