using Microsoft.EntityFrameworkCore;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Infrastructure.Data;
using Rvnx.CRM.Infrastructure.Repositories;
using Rvnx.CRM.Infrastructure.Services;
using Rvnx.CRM.Tests.Helpers;

namespace Rvnx.CRM.Tests.Services;

public class UserSynchronizationTests
{
    private static CRMDbContext GetInMemoryDbContext() => TestDbContextFactory.CreateForSystemUser(ensureCreated: true);

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

    [Fact]
    public async Task SyncUserAsyncUpdatesGroupWhenMissingOnExistingUser()
    {
        using CRMDbContext context = GetInMemoryDbContext();
        Repository repository = new(context);
        UserSynchronizationService service = new(context, repository);

        // Create user via the service so it starts with a proper group.
        await service.SyncUserAsync("sub-nogroup", "nogroup@example.com", "No Group User");

        // Simulate the user losing its group assignment (e.g. migration or data issue).
        Core.Models.User? existingUser = await repository.QueryUnfiltered<Core.Models.User>()
            .FirstOrDefaultAsync(u => u.SubjectId == "sub-nogroup");
        Assert.NotNull(existingUser);
        existingUser.GroupId = null;
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        // SyncUserAsync should detect the missing group and create a new one.
        UserSyncResult? result = await service.SyncUserAsync("sub-nogroup", "nogroup@example.com", "No Group User");

        Assert.NotNull(result);
        Assert.NotNull(result.GroupId);

        context.ChangeTracker.Clear();
        Core.Models.User? user = await repository.QueryUnfiltered<Core.Models.User>()
            .FirstOrDefaultAsync(u => u.SubjectId == "sub-nogroup");
        Assert.NotNull(user);
        Assert.NotNull(user.GroupId);
    }

    [Fact]
    public async Task SyncUserAsyncNoChangeWhenFieldsUnchanged()
    {
        using CRMDbContext context = GetInMemoryDbContext();
        Repository repository = new(context);

        Core.Models.UserGroup group = new() { Name = "Existing Group" };
        Core.Models.User existingUser = new()
        {
            SubjectId = "sub-unchanged",
            Email = "same@example.com",
            DisplayName = "Same Name",
            Group = group
        };
        context.Users!.Add(existingUser);
        await context.SaveChangesAsync();

        int saveCount = 0;
        context.SavedChanges += (_, _) => saveCount++;
        context.ChangeTracker.Clear();

        UserSynchronizationService service = new(context, repository);
        UserSyncResult? result = await service.SyncUserAsync("sub-unchanged", "same@example.com", "Same Name");

        Assert.NotNull(result);
        Assert.Equal(0, saveCount);
    }
}
