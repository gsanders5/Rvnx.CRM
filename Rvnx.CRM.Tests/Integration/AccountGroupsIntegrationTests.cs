using Microsoft.EntityFrameworkCore;
using Moq;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Infrastructure.Data;
using Rvnx.CRM.Infrastructure.Repositories;

namespace Rvnx.CRM.Tests.Integration;

public class AccountGroupsIntegrationTests
{
    [Fact]
    public async Task EntitiesShouldBeFilteredByGroupId()
    {
        Guid group1Id = Guid.NewGuid();
        Guid group2Id = Guid.NewGuid();
        Guid user1Id = Guid.NewGuid();
        Guid user2Id = Guid.NewGuid();

        Mock<ICurrentUserService> mockUserService = new();
        mockUserService.Setup(u => u.UserId).Returns(user1Id);
        mockUserService.Setup(u => u.GroupId).Returns(group1Id);
        mockUserService.Setup(u => u.UserName).Returns("User1");

        DbContextOptions<CRMDbContext> options = new DbContextOptionsBuilder<CRMDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using (CRMDbContext context = new(options, mockUserService.Object))
        {
            // but we want to simulate different groups)

            // Note: SaveChanges uses CurrentUserService to stamp.
            // So to seed Group 2 data, we must manualy set properties OR mock service differently.
            // Since we use in-memory, we can add entities with predefined GroupId.
            // BUT UpdateAuditFields might override them if we are "Added".
            // UpdateAuditFields only stamps if GroupId is NULL. So we can force it.

            Contact c1 = new() { FirstName = "Group1", LastName = "Contact", GroupId = group1Id, UserId = user1Id };
            Contact c2 = new() { FirstName = "Group2", LastName = "Contact", GroupId = group2Id, UserId = user2Id };

            context.Contacts.Add(c1);
            context.Contacts.Add(c2);
            await context.SaveChangesAsync();
        }

        using (CRMDbContext context = new(options, mockUserService.Object))
        {
            Repository repo = new(context);
            List<Contact> results = await repo.ListAsync<Contact>();

            Assert.Single(results);
            Assert.Equal("Group1", results[0].FirstName);
        }
    }

    [Fact]
    public async Task UsersInSameGroupShouldSeeSameData()
    {
        Guid sharedGroupId = Guid.NewGuid();
        Guid userA_Id = Guid.NewGuid();
        Guid userB_Id = Guid.NewGuid();

        DbContextOptions<CRMDbContext> options = new DbContextOptionsBuilder<CRMDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        Mock<ICurrentUserService> mockUserServiceA = new();
        mockUserServiceA.Setup(u => u.UserId).Returns(userA_Id);
        mockUserServiceA.Setup(u => u.GroupId).Returns(sharedGroupId);
        mockUserServiceA.Setup(u => u.UserName).Returns("UserA");

        using (CRMDbContext context = new(options, mockUserServiceA.Object))
        {
            Repository repo = new(context);
            await repo.AddAsync(new Contact { FirstName = "Shared", LastName = "Contact" }); // Auto-stamps GroupId
            await repo.SaveChangesAsync();
        }

        Mock<ICurrentUserService> mockUserServiceB = new();
        mockUserServiceB.Setup(u => u.UserId).Returns(userB_Id);
        mockUserServiceB.Setup(u => u.GroupId).Returns(sharedGroupId); // Same Group
        mockUserServiceB.Setup(u => u.UserName).Returns("UserB");

        using (CRMDbContext context = new(options, mockUserServiceB.Object))
        {
            Repository repo = new(context);
            List<Contact> results = await repo.ListAsync<Contact>();

            Assert.Single(results);
            Assert.Equal("Shared", results[0].FirstName);
            Assert.Equal(sharedGroupId, results[0].GroupId);
            // UserId will belong to User A, but User B can see it because of GroupId
            Assert.Equal(userA_Id, results[0].UserId);
        }
    }

    [Fact]
    public async Task UpdateAuditFieldsShouldStampGroupId()
    {
        Guid groupId = Guid.NewGuid();
        Mock<ICurrentUserService> mockUserService = new();
        mockUserService.Setup(u => u.UserId).Returns(Guid.NewGuid());
        mockUserService.Setup(u => u.GroupId).Returns(groupId);
        mockUserService.Setup(u => u.UserName).Returns("TestUser");

        DbContextOptions<CRMDbContext> options = new DbContextOptionsBuilder<CRMDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using CRMDbContext context = new(options, mockUserService.Object);
        Contact contact = new() { FirstName = "New", LastName = "One" };

        context.Contacts.Add(contact);
        await context.SaveChangesAsync();

        Assert.Equal(groupId, contact.GroupId);
    }
}