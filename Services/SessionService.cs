using Microsoft.EntityFrameworkCore;
using WebApplication1.Model;

namespace WebApplication1.Services
{
    public class SessionService : ISessionService
    {
        private readonly AuthDbContext _context;
        private readonly ILogger<SessionService> _logger;
        private readonly TimeSpan _sessionTimeout = TimeSpan.FromMinutes(1);

        public SessionService(AuthDbContext context, ILogger<SessionService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<string> CreateSessionAsync(string userId, string sessionId, string ipAddress, string userAgent)
        {
            var now = DateTime.UtcNow;
            
            var session = new UserSession
            {
                UserId = userId,
                SessionId = sessionId,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                CreatedAt = now,
                LastActivityAt = now,
                ExpiresAt = now.Add(_sessionTimeout),
                IsActive = true
            };

            _context.UserSessions.Add(session);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Session created for user {UserId} from {IpAddress}", userId, ipAddress);

            return sessionId;
        }

        public async Task<bool> ValidateSessionAsync(string userId, string sessionId)
        {
            var session = await _context.UserSessions
                .FirstOrDefaultAsync(s => s.UserId == userId && s.SessionId == sessionId && s.IsActive);

            if (session == null)
            {
                _logger.LogWarning("Invalid session attempt for user {UserId}", userId);
                return false;
            }

            if (session.ExpiresAt < DateTime.UtcNow)
            {
                _logger.LogInformation("Session expired for user {UserId}", userId);
                session.IsActive = false;
                await _context.SaveChangesAsync();
                return false;
            }

            return true;
        }

        public async Task UpdateSessionActivityAsync(string userId, string sessionId)
        {
            var session = await _context.UserSessions
                .FirstOrDefaultAsync(s => s.UserId == userId && s.SessionId == sessionId && s.IsActive);

            if (session != null)
            {
                session.LastActivityAt = DateTime.UtcNow;
                session.ExpiresAt = DateTime.UtcNow.Add(_sessionTimeout);
                await _context.SaveChangesAsync();
            }
        }

        public async Task InvalidateSessionAsync(string userId, string sessionId)
        {
            var session = await _context.UserSessions
                .FirstOrDefaultAsync(s => s.UserId == userId && s.SessionId == sessionId);

            if (session != null)
            {
                session.IsActive = false;
                await _context.SaveChangesAsync();
                _logger.LogInformation("Session invalidated for user {UserId}", userId);
            }
        }

        public async Task InvalidateAllUserSessionsAsync(string userId)
        {
            var sessions = await _context.UserSessions
                .Where(s => s.UserId == userId && s.IsActive)
                .ToListAsync();

            foreach (var session in sessions)
            {
                session.IsActive = false;
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("All sessions invalidated for user {UserId}", userId);
        }

        public async Task InvalidateAllUserSessionsExceptCurrentAsync(string userId, string currentSessionId)
        {
            var sessions = await _context.UserSessions
                .Where(s => s.UserId == userId && s.IsActive && s.SessionId != currentSessionId)
                .ToListAsync();

            foreach (var session in sessions)
            {
                session.IsActive = false;
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("Invalidated {Count} sessions for user {UserId} (keeping current session)", sessions.Count, userId);
        }

        public async Task<int> GetActiveSessionCountAsync(string userId)
        {
            return await _context.UserSessions
                .CountAsync(s => s.UserId == userId && s.IsActive && s.ExpiresAt > DateTime.UtcNow);
        }

        public async Task<List<UserSession>> GetActiveSessionsAsync(string userId)
        {
            return await _context.UserSessions
                .Where(s => s.UserId == userId && s.IsActive && s.ExpiresAt > DateTime.UtcNow)
                .OrderByDescending(s => s.LastActivityAt)
                .ToListAsync();
        }

        public async Task CleanupExpiredSessionsAsync()
        {
            var expiredSessions = await _context.UserSessions
                .Where(s => s.IsActive && s.ExpiresAt < DateTime.UtcNow)
                .ToListAsync();

            foreach (var session in expiredSessions)
            {
                session.IsActive = false;
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("Cleaned up {Count} expired sessions", expiredSessions.Count);
        }
    }
}
