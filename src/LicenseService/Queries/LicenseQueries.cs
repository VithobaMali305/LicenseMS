using LicenseService.DTOs;
using MediatR;

namespace LicenseService.Queries;

// ── Get licenses for a specific user ──────────────────────────────────────
public record GetLicensesByUserQuery(int UserId) : IRequest<List<LicenseDto>>;

// ── Get ALL licenses (admin only) ─────────────────────────────────────────
public record GetAllLicensesAdminQuery(string? StatusFilter = null) : IRequest<List<LicenseDto>>;

// ── Admin dashboard stats ─────────────────────────────────────────────────
public record GetAdminDashboardStatsQuery : IRequest<DashboardStatsDto>;
