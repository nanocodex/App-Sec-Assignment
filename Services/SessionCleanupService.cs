using WebApplication1.Services;

namespace WebApplication1.Services
{
    public class SessionCleanupService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<SessionCleanupService> _logger;
        private readonly TimeSpan _cleanupInterval = TimeSpan.FromMinutes(10);

        public SessionCleanupService(IServiceProvider serviceProvider, ILogger<SessionCleanupService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Session Cleanup Service started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(_cleanupInterval, stoppingToken);

                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var sessionService = scope.ServiceProvider.GetRequiredService<ISessionService>();
                        await sessionService.CleanupExpiredSessionsAsync();
                    }

                    _logger.LogInformation("Session cleanup completed at {Time}", DateTime.UtcNow);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred during session cleanup");
                }
            }

            _logger.LogInformation("Session Cleanup Service stopped");
        }
    }
}
