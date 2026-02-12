using WebApplication1.Model;

namespace WebApplication1.Services
{
    public class AuditService : IAuditService
    {
        private readonly AuthDbContext _context;
        private readonly ILogger<AuditService> _logger;

        public AuditService(AuthDbContext context, ILogger<AuditService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task LogActivityAsync(string userId, string action, string? details = null, string? ipAddress = null, string? userAgent = null)
        {
            try
            {
                var auditLog = new AuditLog
                {
                    UserId = userId,
                    Action = action,
                    Timestamp = DateTime.UtcNow,
                    IpAddress = ipAddress,
                    UserAgent = userAgent,
                    Details = details
                };

                _context.AuditLogs.Add(auditLog);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log audit activity for user {UserId}, action {Action}", userId, action);
            }
        }
    }
}
