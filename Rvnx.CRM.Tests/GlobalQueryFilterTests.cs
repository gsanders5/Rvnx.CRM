using Microsoft.EntityFrameworkCore;
using Moq;
using Rvnx.CRM.Core.Constants;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Infrastructure.Data;
using Rvnx.CRM.Infrastructure.Repositories;
using Xunit;

namespace Rvnx.CRM.Tests
{
    public class GlobalQueryFilterTests
    {
        private CRMDbContext GetInMemoryDbContext(string? userId)
        {
            DbContextOptions<CRMDbContext> options = new DbContextOptionsBuilder<CRMDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            var mockUserService = new Mock<ICurrentUserService>();
            mockUserService.Setup(u => u.UserId).Returns(userId);
            mockUserService.Setup(u => u.UserName).Returns(userId ?? "System");

            CRMDbContext context = new(options, mockUserService.Object);
            context.Database.EnsureCreated();
            return context;
        }

        [Fact]
        public async Task AuthDisabled_ShouldSeeNullUserIdEntries()
        {
            // Arrange: Simulate Auth Disabled (UserId = null)
            using var context = GetInMemoryDbContext(null);

            // Add entries with different UserIds directly (bypassing Audit logic if possible, or simulating it)
            // Since SaveChanges uses CurrentUser, we need to hack it or use a context with different user first?
            // Actually, we can just set properties manually and bypass SaveChanges logic if we use low-level add?
            // No, SaveChanges calls UpdateAuditFields.

            // Step 1: Add data as "System/Anonymous" (UserId = null)
            // The context is configured with UserId=null, so SaveChanges will set UserId=null.
            context.Contacts.Add(new Contact { FirstName = "Public", LastName = "One" });
            await context.SaveChangesAsync();

            // Step 2: Add data as "UserA" (Simulate another user context)
            // We can manually force UserId if we want, but UpdateAuditFields might overwrite it unless we are careful.
            // UpdateAuditFields only overwrites if string.IsNullOrEmpty.
            var userContact = new Contact { FirstName = "Private", LastName = "Two", UserId = "UserA" };
            context.Contacts.Add(userContact);
            await context.SaveChangesAsync();

            // Verify DB content raw (ignore filters to check setup)
            var all = await context.Contacts.IgnoreQueryFilters().ToListAsync();
            Assert.Equal(2, all.Count);

            // Act: Query as Anonymous (UserId = null)
            var visible = await context.Contacts.ToListAsync();

            // Assert
            Assert.Single(visible);
            Assert.Equal("Public", visible[0].FirstName);
        }

        [Fact]
        public async Task AuthEnabled_ShouldSeeOwnEntries()
        {
            // Arrange: Simulate UserA
            using var context = GetInMemoryDbContext("UserA");

            // Add Own Data
            context.Contacts.Add(new Contact { FirstName = "My", LastName = "Contact" });
            await context.SaveChangesAsync();

            // Add Other Data (UserB)
            // Manually set UserId
            context.Contacts.Add(new Contact { FirstName = "Other", LastName = "Contact", UserId = "UserB" });
            await context.SaveChangesAsync();

            // Add Public Data (UserId = null)
            // Note: Since current user is UserA, UpdateAuditFields will set UserId=UserA unless we force it.
            // But if we force it to null? UpdateAuditFields sets it if IsNullOrEmpty.
            // So we can't easily create a null-UserId record via this context if we are UserA.
            // But let's assume one exists from previous anonymous usage.
            // We can use IgnoreQueryFilters to add it potentially? No, SaveChanges logic is separate.
            // We can trick UpdateAuditFields by setting it to something then setting it back? No.
            // Let's just create a separate context to seed "Public" data.

            // Act
            var visible = await context.Contacts.ToListAsync();

            // Assert: Should see "My Contact". Should NOT see "Other Contact".
            // Should NOT see Public Data? (Requirement: "can only see entries... created by themselves")
            Assert.Contains(visible, c => c.FirstName == "My");
            Assert.DoesNotContain(visible, c => c.FirstName == "Other");
        }

        [Fact]
        public async Task RelationshipTypes_ShouldBeVisibleToAll()
        {
            // RelationshipTypes are seeded with UserId = null (or System).
            // They are excluded from Global Filter.

            using var context = GetInMemoryDbContext("UserA");

            var types = await context.RelationshipTypes.ToListAsync();

            Assert.NotEmpty(types);
        }
    }
}
