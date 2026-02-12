namespace WebApplication1.Services
{
    public interface IAuditService
    {
        Task LogActivityAsync(string userId, string action, string? details = null, string? ipAddress = null, string? userAgent = null);
    }
}
