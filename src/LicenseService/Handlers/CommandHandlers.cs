using LicenseService.Commands;
using LicenseService.Data;
using LicenseService.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LicenseService.Handlers;

// ── ApplyLicenseCommand Handler ───────────────────────────────────────────
public class ApplyLicenseCommandHandler : IRequestHandler<ApplyLicenseCommand, int>
{
    private readonly AppDbContext _db;
    private readonly INotificationService _notify;

    public ApplyLicenseCommandHandler(AppDbContext db, INotificationService notify)
    {
        _db = db;
        _notify = notify;
    }

    public async Task<int> Handle(ApplyLicenseCommand cmd, CancellationToken ct)
    {
        var license = new License
        {
            UserId = cmd.UserId,
            LicenseType = cmd.LicenseType,
            DocumentPath = cmd.DocumentPath,
            Status = "Pending",
            AppliedDate = DateTime.UtcNow
        };

        _db.Licenses.Add(license);
        await _db.SaveChangesAsync(ct);

        var user = await _db.Users.FindAsync(new object[] { cmd.UserId }, ct);
        await _notify.SendAsync(user?.Username ?? "Unknown", license.Id, "Pending");

        return license.Id;
    }
}

// ── UpdateLicenseStatusCommand Handler ────────────────────────────────────
public class UpdateLicenseStatusCommandHandler : IRequestHandler<UpdateLicenseStatusCommand, bool>
{
    private readonly AppDbContext _db;
    private readonly INotificationService _notify;

    public UpdateLicenseStatusCommandHandler(AppDbContext db, INotificationService notify)
    {
        _db = db;
        _notify = notify;
    }

    public async Task<bool> Handle(UpdateLicenseStatusCommand cmd, CancellationToken ct)
    {
        var license = await _db.Licenses
            .Include(l => l.User)
            .FirstOrDefaultAsync(l => l.Id == cmd.LicenseId, ct);

        if (license is null) return false;

        var validStatuses = new[] { "Pending", "Approved", "Rejected" };
        if (!validStatuses.Contains(cmd.NewStatus)) return false;

        license.Status = cmd.NewStatus;
        license.ReviewNotes = cmd.ReviewNotes;
        license.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        await _notify.SendAsync(license.User.Username, license.Id, cmd.NewStatus);

        return true;
    }
}
