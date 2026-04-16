using Microsoft.EntityFrameworkCore;
using Moq;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Infrastructure.Data;
using Rvnx.CRM.Infrastructure.Repositories;
using Rvnx.CRM.Infrastructure.Services;

namespace Rvnx.CRM.Tests.Services;

public class UserSynchronizationTests
{
    private static CRMDbContext GetInMemoryDbContext()
    {
        DbContextOptions<CRMDbContext> options = new DbContextOptionsBuilder<CRMDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        Mock<ICurrentUserService> mockUserService = new();
        mockUserService.Setup(u => u.UserId).Returns((Guid?)null); // Setup as system for syncing
        mockUserService.Setup(u => u.UserName).Returns("System");

        CRMDbContext context = new(options, mockUserService.Object);
        context.Database.EnsureCreated();
        return context;
    }

    [Fact]
    public async Task SyncUserAsyncNewUserShouldCreateUser()
    {
        using CRMDbContext context = GetInMemoryDbContext();
        Repository repository = new(context);
        UserSynchronizationService service = new(context, repository);

        UserSyncResult? result = await service.SyncUserAsync("sub123", "test@example.com", "Test User");

        Core.Models.User? user = await repository.QueryUnfiltered<Core.Models.User>().FirstOrDefaultAsync(u => u.SubjectId == "sub123");
        Assert.NotNull(user);
        Assert.Equal("test@example.com", user.Email);
        Assert.Equal("Test User", user.DisplayName);

        Assert.NotNull(result);
        Assert.Equal(user.Id, result.UserId);
        Assert.Equal("Test User", result.DisplayName);
    }

    [Fact]
    public async Task SyncUserAsyncExistingUserShouldUpdateDetailsAndReturnInternalId()
    {
        using CRMDbContext context = GetInMemoryDbContext();
        Repository repository = new(context);
        UserSynchronizationService service = new(context, repository);

        Core.Models.User existingUser = new()
        {
            SubjectId = "sub456",
            Email = "old@example.com",
            DisplayName = "Old Name",
            Group = new Core.Models.UserGroup { Name = "Old Group" }
        };
        context.Users!.Add(existingUser);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear(); // Clear tracker to simulate fresh request

        UserSyncResult? result = await service.SyncUserAsync("sub456", "new@example.com", "New Name");

        Core.Models.User? user = await repository.QueryUnfiltered<Core.Models.User>().FirstOrDefaultAsync(u => u.SubjectId == "sub456");
        Assert.NotNull(user);
        Assert.Equal("new@example.com", user.Email); // Should update
        Assert.Equal("New Name", user.DisplayName); // Should update

        Assert.NotNull(result);
        Assert.Equal(existingUser.Id, result.UserId);
    }

    [Fact]
    public async Task SyncUserAsyncShouldDoNothingWhenNoSubject()
    {
        using CRMDbContext context = GetInMemoryDbContext();
        Repository repository = new(context);
        UserSynchronizationService service = new(context, repository);

        UserSyncResult? result = await service.SyncUserAsync(string.Empty, "test@example.com", null);

        int userCount = await repository.QueryUnfiltered<Core.Models.User>().CountAsync();
        Assert.Equal(0, userCount);
        Assert.Null(result);
    }

    [Fact]
    public async Task SyncUserAsyncShouldReturnSameResultWhenCalledMultipleTimes()
    {
        using CRMDbContext context = GetInMemoryDbContext();
        Repository repository = new(context);
        UserSynchronizationService service = new(context, repository);

        UserSyncResult? result1 = await service.SyncUserAsync("sub-multiple", "test@example.com", null);
        UserSyncResult? result2 = await service.SyncUserAsync("sub-multiple", "test@example.com", null);
        UserSyncResult? result3 = await service.SyncUserAsync("sub-multiple", "test@example.com", null);

        Assert.NotNull(result1);
        Assert.NotNull(result2);
        Assert.NotNull(result3);
        Assert.Equal(result1.UserId, result2.UserId);
        Assert.Equal(result2.UserId, result3.UserId);
    }
}