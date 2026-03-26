namespace LicenseService.DTOs;

public record LicenseDto(
    int Id,
    int UserId,
    string Username,
    string LicenseType,
    string Status,
    string? DocumentPath,
    DateTime AppliedDate,
    DateTime? UpdatedAt,
    string? ReviewNotes
);

public record DashboardStatsDto(
    int TotalUsers,
    int TotalLicenses,
    int PendingCount,
    int ApprovedCount,
    int RejectedCount
);

public record RegisterRequest(string Username, string Password, string ConfirmPassword);

public record LoginRequest(string Username, string Password);

public record LoginResponse(string Token, string Role, string Username, int UserId);

public record ApplyLicenseRequest(int UserId, string LicenseType, string? DocumentPath);

public record UpdateStatusRequest(string NewStatus, string? ReviewNotes);
