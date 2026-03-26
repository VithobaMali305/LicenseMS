using LicenseService.Commands;
using LicenseService.Data;
using LicenseService.Handlers;
using LicenseService.Models;
using LicenseService.Queries;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LicenseService.UnitTests;

// ── Test fixture: in-memory DB ────────────────────────────────────────────
public static class DbFactory
{
    public static AppDbContext Create(string dbName = "")
    {
        var opts = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(string.IsNullOrEmpty(dbName) ? Guid.NewGuid().ToString() : dbName)
            .Options;
        return new AppDbContext(opts);
    }

    public static AppDbContext CreateWithSeedData()
    {
        var db = Create();
        db.Users.AddRange(
            new User { Id = 1, Username = "alice", PasswordHash = "hash", Role = "User" },
            new User { Id = 2, Username = "bob",   PasswordHash = "hash", Role = "User" },
            new User { Id = 3, Username = "admin", PasswordHash = "hash", Role = "Admin" }
        );
        db.Licenses.AddRange(
            new License { Id = 1, UserId = 1, LicenseType = "Business License",  Status = "Pending"  },
            new License { Id = 2, UserId = 1, LicenseType = "Trade License",      Status = "Approved" },
            new License { Id = 3, UserId = 2, LicenseType = "Vehicle Registration",Status = "Rejected" }
        );
        db.SaveChanges();
        return db;
    }
}

// ════════════════════════════════════════════════════════════════════════════
// COMMAND HANDLER TESTS
// ════════════════════════════════════════════════════════════════════════════
public class ApplyLicenseCommandHandlerTests
{
    private readonly Mock<INotificationService> _notifyMock = new();

    [Fact]
    public async Task Handle_ValidCommand_CreatesLicenseWithPendingStatus()
    {
        // Arrange
        var db = DbFactory.Create();
        db.Users.Add(new User { Id = 10, Username = "testuser", PasswordHash = "h", Role = "User" });
        await db.SaveChangesAsync();

        var handler = new ApplyLicenseCommandHandler(db, _notifyMock.Object);
        var cmd = new ApplyLicenseCommand(10, "Business License", "uploads/test.pdf");

        // Act
        var id = await handler.Handle(cmd, CancellationToken.None);

        // Assert
        Assert.True(id > 0);
        var saved = await db.Licenses.FindAsync(id);
        Assert.NotNull(saved);
        Assert.Equal("Pending",          saved!.Status);
        Assert.Equal("Business License", saved.LicenseType);
        Assert.Equal(10,                 saved.UserId);
        Assert.Equal("uploads/test.pdf", saved.DocumentPath);
    }

    [Fact]
    public async Task Handle_ValidCommand_TriggersNotification()
    {
        // Arrange
        var db = DbFactory.Create();
        db.Users.Add(new User { Id = 11, Username = "notifyuser", PasswordHash = "h", Role = "User" });
        await db.SaveChangesAsync();

        var handler = new ApplyLicenseCommandHandler(db, _notifyMock.Object);

        // Act
        await handler.Handle(new ApplyLicenseCommand(11, "Trade License", null), CancellationToken.None);

        // Assert
        _notifyMock.Verify(n => n.SendAsync("notifyuser", It.IsAny<int>(), "Pending"), Times.Once);
    }
}

public class UpdateLicenseStatusCommandHandlerTests
{
    private readonly Mock<INotificationService> _notifyMock = new();

    [Theory]
    [InlineData("Approved")]
    [InlineData("Rejected")]
    public async Task Handle_ValidStatus_UpdatesLicense(string newStatus)
    {
        // Arrange
        var db = DbFactory.CreateWithSeedData();
        var handler = new UpdateLicenseStatusCommandHandler(db, _notifyMock.Object);
        var cmd = new UpdateLicenseStatusCommand(1, newStatus, "Test notes", 3);

        // Act
        var result = await handler.Handle(cmd, CancellationToken.None);

        // Assert
        Assert.True(result);
        var updated = await db.Licenses.FindAsync(1);
        Assert.Equal(newStatus,    updated!.Status);
        Assert.Equal("Test notes", updated.ReviewNotes);
        Assert.NotNull(updated.UpdatedAt);
    }

    [Fact]
    public async Task Handle_InvalidLicenseId_ReturnsFalse()
    {
        var db = DbFactory.CreateWithSeedData();
        var handler = new UpdateLicenseStatusCommandHandler(db, _notifyMock.Object);

        var result = await handler.Handle(
            new UpdateLicenseStatusCommand(999, "Approved", null, 3),
            CancellationToken.None);

        Assert.False(result);
    }

    [Fact]
    public async Task Handle_InvalidStatus_ReturnsFalse()
    {
        var db = DbFactory.CreateWithSeedData();
        var handler = new UpdateLicenseStatusCommandHandler(db, _notifyMock.Object);

        var result = await handler.Handle(
            new UpdateLicenseStatusCommand(1, "InvalidStatus", null, 3),
            CancellationToken.None);

        Assert.False(result);
    }
}

// ════════════════════════════════════════════════════════════════════════════
// QUERY HANDLER TESTS
// ════════════════════════════════════════════════════════════════════════════
public class GetLicensesByUserQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsOnlyUserLicenses()
    {
        var db = DbFactory.CreateWithSeedData();
        var handler = new GetLicensesByUserQueryHandler(db);

        var result = await handler.Handle(new GetLicensesByUserQuery(1), CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.All(result, l => Assert.Equal(1, l.UserId));
    }

    [Fact]
    public async Task Handle_UnknownUser_ReturnsEmptyList()
    {
        var db = DbFactory.CreateWithSeedData();
        var handler = new GetLicensesByUserQueryHandler(db);

        var result = await handler.Handle(new GetLicensesByUserQuery(999), CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task Handle_ResultsOrderedByDateDescending()
    {
        var db = DbFactory.Create();
        db.Users.Add(new User { Id = 5, Username = "u5", PasswordHash = "h", Role = "User" });
        db.Licenses.AddRange(
            new License { Id = 10, UserId = 5, LicenseType = "A", Status = "Pending", AppliedDate = DateTime.UtcNow.AddDays(-5) },
            new License { Id = 11, UserId = 5, LicenseType = "B", Status = "Pending", AppliedDate = DateTime.UtcNow }
        );
        await db.SaveChangesAsync();

        var handler = new GetLicensesByUserQueryHandler(db);
        var result  = await handler.Handle(new GetLicensesByUserQuery(5), CancellationToken.None);

        Assert.Equal(11, result[0].Id);  // Most recent first
        Assert.Equal(10, result[1].Id);
    }
}

public class GetAllLicensesAdminQueryHandlerTests
{
    [Fact]
    public async Task Handle_NoFilter_ReturnsAllLicenses()
    {
        var db = DbFactory.CreateWithSeedData();
        var handler = new GetAllLicensesAdminQueryHandler(db);

        var result = await handler.Handle(new GetAllLicensesAdminQuery(), CancellationToken.None);

        Assert.Equal(3, result.Count);
    }

    [Fact]
    public async Task Handle_WithStatusFilter_ReturnsFilteredLicenses()
    {
        var db = DbFactory.CreateWithSeedData();
        var handler = new GetAllLicensesAdminQueryHandler(db);

        var result = await handler.Handle(
            new GetAllLicensesAdminQuery("Pending"), CancellationToken.None);

        Assert.Single(result);
        Assert.All(result, l => Assert.Equal("Pending", l.Status));
    }
}

public class GetAdminDashboardStatsQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsCorrectCounts()
    {
        var db = DbFactory.CreateWithSeedData();
        var handler = new GetAdminDashboardStatsQueryHandler(db);

        var stats = await handler.Handle(new GetAdminDashboardStatsQuery(), CancellationToken.None);

        Assert.Equal(3, stats.TotalUsers);
        Assert.Equal(3, stats.TotalLicenses);
        Assert.Equal(1, stats.PendingCount);
        Assert.Equal(1, stats.ApprovedCount);
        Assert.Equal(1, stats.RejectedCount);
    }
}

// ════════════════════════════════════════════════════════════════════════════
// NOTIFICATION SERVICE TESTS
// ════════════════════════════════════════════════════════════════════════════
public class ConsoleNotificationServiceTests
{
    [Fact]
    public async Task SendAsync_DoesNotThrow()
    {
        var logger  = new Mock<ILogger<ConsoleNotificationService>>();
        var service = new ConsoleNotificationService(logger.Object);

        var ex = await Record.ExceptionAsync(
            () => service.SendAsync("alice", 42, "Approved"));

        Assert.Null(ex);
    }
}
