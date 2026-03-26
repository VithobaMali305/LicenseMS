using LicenseService.DTOs;
using MediatR;

namespace LicenseService.Commands;

// ── Apply License ──────────────────────────────────────────────────────────
public record ApplyLicenseCommand(
    int UserId,
    string LicenseType,
    string? DocumentPath
) : IRequest<int>;   // returns new LicenseId

// ── Update License Status ──────────────────────────────────────────────────
public record UpdateLicenseStatusCommand(
    int LicenseId,
    string NewStatus,
    string? ReviewNotes,
    int AdminId
) : IRequest<bool>;
