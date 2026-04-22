using Microsoft.EntityFrameworkCore;
using Moq;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Infrastructure.Data;

namespace Rvnx.CRM.Tests.Helpers;

/// <summary>
/// Shared factory for creating in-memory <see cref="CRMDbContext"/> instances used by unit tests.
/// Centralises the duplicated setup previously copied in each test class.
/// </summary>
internal static class TestDbContextFactory
{
    /// <summary>
    /// The default test user id used by most test fixtures (kept as a constant for reuse).
    /// </summary>
    public static readonly Guid DefaultUserId = Guid.Parse("c5b50a20-34b2-44b2-8b9c-aa4135f60938");

    /// <summary>
    /// Creates an in-memory <see cref="CRMDbContext"/> bound to a unique database and a mocked
    /// <see cref="ICurrentUserService"/>. The caller is responsible for disposing the context.
    /// </summary>
    /// <param name="userId">Current user id. <c>null</c> simulates a System/background user.</param>
    /// <param name="userName">Current user name. Defaults to "test-user" when a user id is supplied, otherwise "System".</param>
    /// <param name="groupId">Current group id for the mocked user.</param>
    /// <param name="mockUserService">Outputs the underlying mock so tests can further tweak it.</param>
    /// <param name="ensureCreated">When true, calls <c>Database.EnsureCreated()</c> before returning.</param>
    public static CRMDbContext Create(
        Guid? userId,
        string? userName,
        Guid? groupId,
        out Mock<ICurrentUserService> mockUserService,
        bool ensureCreated = false)
    {
        DbContextOptions<CRMDbContext> options = new DbContextOptionsBuilder<CRMDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        mockUserService = new Mock<ICurrentUserService>();
        mockUserService.Setup(u => u.UserId).Returns(userId);
        mockUserService.Setup(u => u.GroupId).Returns(groupId);
        mockUserService.Setup(u => u.UserName).Returns(userName ?? (userId.HasValue ? "test-user" : "System"));

        CRMDbContext context = new(options, mockUserService.Object);
        if (ensureCreated)
        {
            context.Database.EnsureCreated();
        }
        return context;
    }

    /// <summary>
    /// Creates an in-memory context impersonating the default test user. The mock is discarded.
    /// </summary>
    public static CRMDbContext CreateForDefaultUser(bool ensureCreated = false)
        => Create(DefaultUserId, "test-user", null, out _, ensureCreated);

    /// <summary>
    /// Creates an in-memory context impersonating the System user (no user id). The mock is discarded.
    /// </summary>
    public static CRMDbContext CreateForSystemUser(bool ensureCreated = false)
        => Create(null, "System", null, out _, ensureCreated);
}
