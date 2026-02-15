using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Moq;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Infrastructure.Data;
using Rvnx.CRM.Web.Services;
using Xunit;

namespace Rvnx.CRM.Tests
{
    public class UserSynchronizationTests
    {
        private CRMDbContext GetInMemoryDbContext()
        {
            DbContextOptions<CRMDbContext> options = new DbContextOptionsBuilder<CRMDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            var mockUserService = new Mock<ICurrentUserService>();
            mockUserService.Setup(u => u.UserId).Returns((string?)null); // Setup as system for syncing
            mockUserService.Setup(u => u.UserName).Returns("System");

            CRMDbContext context = new(options, mockUserService.Object);
            context.Database.EnsureCreated();
            return context;
        }

        [Fact]
        public async Task SyncUserAsync_NewUser_ShouldCreateUser()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var service = new UserSynchronizationService(context);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "sub123"),
                new Claim(ClaimTypes.Email, "test@example.com"),
                new Claim(ClaimTypes.Name, "Test User")
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var principal = new ClaimsPrincipal(identity);

            // Act
            await service.SyncUserAsync(principal);

            // Assert
            var user = await context.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.SubjectId == "sub123");
            Assert.NotNull(user);
            Assert.Equal("test@example.com", user.Email);
            Assert.Equal("Test User", user.DisplayName);

            // Check Principal modification
            Assert.Contains(principal.Claims, c => c.Type == ClaimTypes.NameIdentifier && c.Value == user.Id.ToString());
        }

        [Fact]
        public async Task SyncUserAsync_ExistingUser_ShouldUpdateDetailsAndMapId()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var service = new UserSynchronizationService(context);

            // Pre-create user
            var existingUser = new Core.Models.User
            {
                SubjectId = "sub456",
                Email = "old@example.com",
                DisplayName = "Old Name",
                UserId = "System"
            };
            context.Users.Add(existingUser);
            await context.SaveChangesAsync();

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "sub456"),
                new Claim(ClaimTypes.Email, "new@example.com"),
                new Claim(ClaimTypes.Name, "New Name")
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var principal = new ClaimsPrincipal(identity);

            // Act
            await service.SyncUserAsync(principal);

            // Assert
            var user = await context.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.SubjectId == "sub456");
            Assert.NotNull(user);
            Assert.Equal("new@example.com", user.Email); // Should update
            Assert.Equal("New Name", user.DisplayName); // Should update

            // Check Principal modification
            Assert.DoesNotContain(principal.Claims, c => c.Type == ClaimTypes.NameIdentifier && c.Value == "sub456");
            Assert.Contains(principal.Claims, c => c.Type == ClaimTypes.NameIdentifier && c.Value == existingUser.Id.ToString());
        }
    }
}
