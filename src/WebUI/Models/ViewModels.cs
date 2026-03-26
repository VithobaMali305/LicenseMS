using System.ComponentModel.DataAnnotations;

namespace WebUI.Models;

// ── Auth ──────────────────────────────────────────────────────────────────
public class LoginViewModel
{
    [Required] public string Username { get; set; } = string.Empty;
    [Required][DataType(DataType.Password)] public string Password { get; set; } = string.Empty;
}

public class RegisterViewModel
{
    [Required][MinLength(3)] public string Username { get; set; } = string.Empty;

    [Required][MinLength(8)]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required][DataType(DataType.Password)]
    [Compare("Password", ErrorMessage = "Passwords do not match.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

// ── License ───────────────────────────────────────────────────────────────
public class LicenseViewModel
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string LicenseType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? DocumentPath { get; set; }
    public DateTime AppliedDate { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? ReviewNotes { get; set; }
}

public class ApplyLicenseViewModel
{
    [Required(ErrorMessage = "Please select a license type.")]
    public string LicenseType { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please upload a supporting document.")]
    public IFormFile? Document { get; set; }

    public static List<string> LicenseTypes => new()
    {
        "Business License",
        "Vehicle Registration",
        "Trade License",
        "Food Safety License",
        "Building Permit",
        "Import/Export License",
        "Professional License"
    };
}

public class UpdateStatusViewModel
{
    [Required] public int LicenseId { get; set; }
    [Required] public string NewStatus { get; set; } = string.Empty;
    public string? ReviewNotes { get; set; }
}

// ── Dashboard ─────────────────────────────────────────────────────────────
public class AdminDashboardViewModel
{
    public int TotalUsers { get; set; }
    public int TotalLicenses { get; set; }
    public int PendingCount { get; set; }
    public int ApprovedCount { get; set; }
    public int RejectedCount { get; set; }
    public List<LicenseViewModel> Licenses { get; set; } = new();
    public string? StatusFilter { get; set; }
}

public class UserDashboardViewModel
{
    public string Username { get; set; } = string.Empty;
    public List<LicenseViewModel> Licenses { get; set; } = new();
}
