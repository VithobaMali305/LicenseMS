using LicenseService.Data;
using LicenseService.DTOs;
using LicenseService.Models;
using LicenseService.Queries;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LicenseService.Handlers;

// ── GetLicensesByUserQuery Handler ────────────────────────────────────────
public class GetLicensesByUserQueryHandler : IRequestHandler<GetLicensesByUserQuery, List<LicenseDto>>
{
    private readonly AppDbContext _db;
    public GetLicensesByUserQueryHandler(AppDbContext db) => _db = db;

    public async Task<List<LicenseDto>> Handle(GetLicensesByUserQuery q, CancellationToken ct)
        => await _db.Licenses
            .Include(l => l.User)
            .Where(l => l.UserId == q.UserId)
            .OrderByDescending(l => l.AppliedDate)
            .Select(l => ToDto(l))
            .ToListAsync(ct);

    private static LicenseDto ToDto(License l) => new(
        l.Id, l.UserId, l.User.Username, l.LicenseType,
        l.Status, l.DocumentPath, l.AppliedDate, l.UpdatedAt, l.ReviewNotes);
}

// ── GetAllLicensesAdminQuery Handler ──────────────────────────────────────
public class GetAllLicensesAdminQueryHandler : IRequestHandler<GetAllLicensesAdminQuery, List<LicenseDto>>
{
    private readonly AppDbContext _db;
    public GetAllLicensesAdminQueryHandler(AppDbContext db) => _db = db;

    public async Task<List<LicenseDto>> Handle(GetAllLicensesAdminQuery q, CancellationToken ct)
    {
        var query = _db.Licenses.Include(l => l.User).AsQueryable();
        if (!string.IsNullOrEmpty(q.StatusFilter))
            query = query.Where(l => l.Status == q.StatusFilter);

        return await query
            .OrderByDescending(l => l.AppliedDate)
            .Select(l => new LicenseDto(
                l.Id, l.UserId, l.User.Username, l.LicenseType,
                l.Status, l.DocumentPath, l.AppliedDate, l.UpdatedAt, l.ReviewNotes))
            .ToListAsync(ct);
    }
}

// ── GetAdminDashboardStatsQuery Handler ───────────────────────────────────
public class GetAdminDashboardStatsQueryHandler : IRequestHandler<GetAdminDashboardStatsQuery, DashboardStatsDto>
{
    private readonly AppDbContext _db;
    public GetAdminDashboardStatsQueryHandler(AppDbContext db) => _db = db;

    public async Task<DashboardStatsDto> Handle(GetAdminDashboardStatsQuery _, CancellationToken ct)
    {
        var totalUsers    = await _db.Users.CountAsync(ct);
        var totalLicenses = await _db.Licenses.CountAsync(ct);
        var pending       = await _db.Licenses.CountAsync(l => l.Status == "Pending", ct);
        var approved      = await _db.Licenses.CountAsync(l => l.Status == "Approved", ct);
        var rejected      = await _db.Licenses.CountAsync(l => l.Status == "Rejected", ct);
        return new DashboardStatsDto(totalUsers, totalLicenses, pending, approved, rejected);
    }
}
