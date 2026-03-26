using Microsoft.EntityFrameworkCore.Migrations;

/*
 * ═══════════════════════════════════════════════════════════════════════
 *  HOW TO GENERATE / APPLY MIGRATIONS
 * ═══════════════════════════════════════════════════════════════════════
 *  From the solution root:
 *
 *  1. Add migration (first time):
 *     dotnet ef migrations add InitialCreate --project src/LicenseService
 *
 *  2. Apply to database:
 *     dotnet ef database update --project src/LicenseService
 *
 *  OR — the Program.cs calls db.Database.Migrate() on startup
 *       so migrations run automatically when the service starts.
 *
 *  Default connection (LocalDB):
 *     Server=(localdb)\mssqllocaldb;Database=LicenseManagementDB;Trusted_Connection=True
 *
 *  Change in src/LicenseService/appsettings.json → ConnectionStrings:DefaultConnection
 * ═══════════════════════════════════════════════════════════════════════
 */
namespace LicenseService.Data.Migrations
{
    public static class MigrationNotes
    {
        public const string Info = "Run: dotnet ef migrations add InitialCreate --project src/LicenseService";
    }
}
