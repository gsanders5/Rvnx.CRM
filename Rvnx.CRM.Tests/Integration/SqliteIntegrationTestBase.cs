using Microsoft.EntityFrameworkCore;
using Moq;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Infrastructure.Data;

namespace Rvnx.CRM.Tests.Integration;

public abstract class SqliteIntegrationTestBase : IDisposable
{
    private readonly string _dbPath;
    protected CRMDbContext Context { get; }
    protected Mock<ICurrentUserService> MockUserService { get; }

    protected SqliteIntegrationTestBase(Guid? userId = null)
    {
        // Use a unique file for each test class/instance to ensure isolation
        _dbPath = $"test_{Guid.NewGuid()}.db";
        string connectionString = $"Data Source={_dbPath}";

        DbContextOptions<CRMDbContext> options = new DbContextOptionsBuilder<CRMDbContext>()
            .UseSqlite(connectionString)
            .Options;

        MockUserService = new Mock<ICurrentUserService>();
        MockUserService.Setup(u => u.UserId).Returns(userId);
        MockUserService.Setup(u => u.UserName).Returns(userId.HasValue ? userId.Value.ToString() : "System");

        Context = new CRMDbContext(options, MockUserService.Object);

        Context.Database.EnsureDeleted();
        Context.Database.EnsureCreated();
    }

    public void Dispose()
    {
        Context.Database.EnsureDeleted();
        Context.Dispose();

        // Try to delete the file if EF didn't (EnsureDeleted usually handles it for Sqlite)
        if (File.Exists(_dbPath))
        {
            try
            {
                File.Delete(_dbPath);
            }
            catch { /* Ignore if file locked, OS will clean up eventually */ }
        }

        GC.SuppressFinalize(this);
    }
}
