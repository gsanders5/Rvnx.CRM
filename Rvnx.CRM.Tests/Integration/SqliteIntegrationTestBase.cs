using Microsoft.EntityFrameworkCore;
using Moq;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Infrastructure.Data;

namespace Rvnx.CRM.Tests.Integration;

public abstract class SqliteIntegrationTestBase : IDisposable
{
    private readonly string _dbPath;
    protected readonly CRMDbContext _context;
    protected readonly Mock<ICurrentUserService> _mockUserService;

    protected SqliteIntegrationTestBase(string? userId = "System")
    {
        // Use a unique file for each test class/instance to ensure isolation
        _dbPath = $"test_{Guid.NewGuid()}.db";
        string connectionString = $"Data Source={_dbPath}";

        DbContextOptions<CRMDbContext> options = new DbContextOptionsBuilder<CRMDbContext>()
            .UseSqlite(connectionString)
            .Options;

        _mockUserService = new Mock<ICurrentUserService>();
        _mockUserService.Setup(u => u.UserId).Returns(userId);
        _mockUserService.Setup(u => u.UserName).Returns(userId ?? "System");

        _context = new CRMDbContext(options, _mockUserService.Object);

        // Ensure clean slate
        _context.Database.EnsureDeleted();
        _context.Database.EnsureCreated();
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();

        // Try to delete the file if EF didn't (EnsureDeleted usually handles it for Sqlite)
        if (File.Exists(_dbPath))
        {
            try
            {
                File.Delete(_dbPath);
            }
            catch { /* Ignore if file locked, OS will clean up eventually */ }
        }
    }
}
