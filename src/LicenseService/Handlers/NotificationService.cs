// ── INotificationService ─────────────────────────────────────────────────
public interface INotificationService
{
    Task SendAsync(string username, int licenseId, string status);
}

// ── Console (Mock) Implementation ─────────────────────────────────────────
namespace LicenseService.Handlers
{
    public class ConsoleNotificationService : INotificationService
    {
        private readonly ILogger<ConsoleNotificationService> _logger;
        public ConsoleNotificationService(ILogger<ConsoleNotificationService> logger)
            => _logger = logger;

        public Task SendAsync(string username, int licenseId, string status)
        {
            _logger.LogInformation(
                "[NOTIFY] Email sent to {Username} for Application ID {LicenseId} — Status: {Status}",
                username, licenseId, status);
            return Task.CompletedTask;
        }
    }
}
