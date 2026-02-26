using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Infrastructure.Data;
using Rvnx.CRM.Web.Controllers;

namespace Rvnx.CRM.Tests.Integration;

public class MergeAccountsTests
{
    private static CRMDbContext GetInMemoryDbContext(ICurrentUserService currentUserService)
    {
        DbContextOptions<CRMDbContext> options = new DbContextOptionsBuilder<CRMDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new CRMDbContext(options, currentUserService);
    }

    [Fact]
    public async Task MergeAccountsShouldMoveEntitiesAndUsersToKeptGroup()
    {
        // Arrange
        Guid adminId = Guid.NewGuid();
        Guid group1Id = Guid.NewGuid();
        Guid group2Id = Guid.NewGuid();
        Guid user1Id = Guid.NewGuid();
        Guid user2Id = Guid.NewGuid();

        Mock<ICurrentUserService> mockUserService = new();
        mockUserService.Setup(u => u.UserId).Returns(adminId);
        mockUserService.Setup(u => u.GroupId).Returns(Guid.NewGuid()); // Admin has their own group
        mockUserService.Setup(u => u.UserName).Returns("Admin");

        using CRMDbContext context = GetInMemoryDbContext(mockUserService.Object);

        // Setup Admin User
        context.Users.Add(new User { Id = adminId, Email = "admin@example.com", IsAdministrator = true, GroupId = mockUserService.Object.GroupId, SubjectId = "admin" });

        // Setup Group 1 (Kept - has more members)
        UserGroup group1 = new() { Id = group1Id, Name = "Group 1" };
        User user1 = new() { Id = user1Id, Email = "u1@example.com", Group = group1, GroupId = group1Id, SubjectId = "u1" };
        User user1b = new() { Id = Guid.NewGuid(), Email = "u1b@example.com", Group = group1, GroupId = group1Id, SubjectId = "u1b" }; // Extra member to win tie
        context.UserGroups.Add(group1);
        context.Users.Add(user1);
        context.Users.Add(user1b);

        // Setup Group 2 (Discarded)
        UserGroup group2 = new() { Id = group2Id, Name = "Group 2" };
        User user2 = new() { Id = user2Id, Email = "u2@example.com", Group = group2, GroupId = group2Id, SubjectId = "u2" };
        context.UserGroups.Add(group2);
        context.Users.Add(user2);

        // Add Entities for Group 2
        Contact contact2 = new() { FirstName = "G2", LastName = "Contact", GroupId = group2Id, UserId = user2Id };
        context.Contacts.Add(contact2);

        await context.SaveChangesAsync();

        Mock<IDebugDataService> mockDebugService = new();
        Mock<IHostEnvironment> mockEnv = new();
        mockEnv.Setup(e => e.EnvironmentName).Returns("Development");
        Mock<ILogger<DebugOperationsController>> mockLogger = new();

        DebugOperationsController controller = new(
            mockDebugService.Object,
            mockEnv.Object,
            context,
            mockUserService.Object,
            mockLogger.Object);

        // Setup TempData
        controller.TempData = new Microsoft.AspNetCore.Mvc.ViewFeatures.TempDataDictionary(new DefaultHttpContext(), Mock.Of<Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataProvider>());

        // Act
        // Merge user2 (Group 2) into user1 (Group 1)
        IActionResult result = await controller.MergeAccounts(user1Id, user2Id, "MERGE");

        // Assert
        Assert.IsType<RedirectToActionResult>(result);

        // Verify Data
        context.ChangeTracker.Clear();

        // 1. Discarded group should be deleted
        UserGroup? g2 = await context.UserGroups.IgnoreQueryFilters().FirstOrDefaultAsync(g => g.Id == group2Id);
        Assert.Null(g2);

        // 2. User 2 should now be in Group 1
        User? u2 = await context.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == user2Id);
        Assert.NotNull(u2);
        Assert.Equal(group1Id, u2!.GroupId);

        // 3. Contact entities from Group 2 should now belong to Group 1
        Contact? c2 = await context.Contacts.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.Id == contact2.Id);
        Assert.NotNull(c2);
        Assert.Equal(group1Id, c2!.GroupId);

        // 4. Group 1 should still exist
        UserGroup? g1 = await context.UserGroups.IgnoreQueryFilters().FirstOrDefaultAsync(g => g.Id == group1Id);
        Assert.NotNull(g1);
    }

    [Fact]
    public async Task MergeAccountsShouldFailIfNonAdmin()
    {
        // Arrange
        Guid regularUserId = Guid.NewGuid();
        Mock<ICurrentUserService> mockUserService = new();
        mockUserService.Setup(u => u.UserId).Returns(regularUserId);

        using CRMDbContext context = GetInMemoryDbContext(mockUserService.Object);
        context.Users.Add(new User { Id = regularUserId, Email = "regular@example.com", IsAdministrator = false, SubjectId = "reg" });
        await context.SaveChangesAsync();

        DebugOperationsController controller = new(
            new Mock<IDebugDataService>().Object,
            new Mock<IHostEnvironment>().Object,
            context,
            mockUserService.Object,
            new Mock<ILogger<DebugOperationsController>>().Object);

        // Act
        IActionResult result = await controller.MergeAccounts(Guid.NewGuid(), Guid.NewGuid(), "MERGE");

        // Assert
        Assert.IsType<ForbidResult>(result);
    }
}
