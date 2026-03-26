using Microsoft.Extensions.Logging;

namespace NotificationService;

// ── Contract ──────────────────────────────────────────────────────────────
public interface INotificationService
{
    Task SendAsync(string username, int licenseId, string status);
}

// ── Console / Mock (used in development) ─────────────────────────────────
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

// ── SMTP Stub (swap in for production) ───────────────────────────────────
public class SmtpNotificationService : INotificationService
{
    private readonly ILogger<SmtpNotificationService> _logger;

    public SmtpNotificationService(ILogger<SmtpNotificationService> logger)
        => _logger = logger;

    public async Task SendAsync(string username, int licenseId, string status)
    {
        // TODO: Replace with real SmtpClient / SendGrid / Mailkit implementation.
        // Example body:
        //   Subject: "License Application #{licenseId} Update"
        //   Body:    "Dear {username}, your application status is now: {status}."
        _logger.LogInformation(
            "[SMTP-STUB] Would send email to {Username} — License #{LicenseId} is {Status}.",
            username, licenseId, status);
        await Task.CompletedTask;
    }
}
