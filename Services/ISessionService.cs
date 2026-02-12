namespace WebApplication1.Services
{
    public interface ISessionService
    {
        Task<string> CreateSessionAsync(string userId, string sessionId, string ipAddress, string userAgent);
        Task<bool> ValidateSessionAsync(string userId, string sessionId);
        Task UpdateSessionActivityAsync(string userId, string sessionId);
        Task InvalidateSessionAsync(string userId, string sessionId);
        Task InvalidateAllUserSessionsAsync(string userId);
        Task InvalidateAllUserSessionsExceptCurrentAsync(string userId, string currentSessionId);
        Task<int> GetActiveSessionCountAsync(string userId);
        Task<List<Model.UserSession>> GetActiveSessionsAsync(string userId);
        Task CleanupExpiredSessionsAsync();
    }
}
