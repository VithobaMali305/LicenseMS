namespace LicenseService.Models;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = "User";   // "Admin" | "User"
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<License> Licenses { get; set; } = new List<License>();
}

public class License
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string LicenseType { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";  // "Pending"|"Approved"|"Rejected"
    public string? DocumentPath { get; set; }
    public DateTime AppliedDate { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public string? ReviewNotes { get; set; }

    // Navigation
    public User User { get; set; } = null!;
}
