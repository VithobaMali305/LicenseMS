using LicenseService.Models;
using Microsoft.EntityFrameworkCore;

namespace LicenseService.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<License> Licenses => Set<License>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        // ── Users ─────────────────────────────────────────────────────────
        mb.Entity<User>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Username).HasMaxLength(100).IsRequired();
            e.HasIndex(x => x.Username).IsUnique();
            e.Property(x => x.PasswordHash).HasMaxLength(512).IsRequired();
            e.Property(x => x.Role).HasMaxLength(20).IsRequired();
            e.HasCheckConstraint("CK_Users_Role", "Role IN ('Admin','User')");
        });

        // ── Licenses ──────────────────────────────────────────────────────
        mb.Entity<License>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.LicenseType).HasMaxLength(100).IsRequired();
            e.Property(x => x.Status).HasMaxLength(20).IsRequired().HasDefaultValue("Pending");
            e.Property(x => x.DocumentPath).HasMaxLength(500);
            e.Property(x => x.ReviewNotes).HasColumnType("nvarchar(max)");
            e.HasCheckConstraint("CK_Licenses_Status", "Status IN ('Pending','Approved','Rejected')");
            e.HasOne(x => x.User)
             .WithMany(u => u.Licenses)
             .HasForeignKey(x => x.UserId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ── Seed admin user (password: Admin@123) ─────────────────────────
        mb.Entity<User>().HasData(new User
        {
            Id = 1,
            Username = "admin",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
            Role = "Admin",
            CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        });
    }
}
