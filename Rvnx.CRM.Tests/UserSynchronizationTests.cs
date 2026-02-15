using Microsoft.EntityFrameworkCore;
using Moq;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Infrastructure.Data;
using Rvnx.CRM.Web.Services;
using System.Security.Claims;

namespace Rvnx.CRM.Tests
{
    public class UserSynchronizationTests
    {
        private CRMDbContext GetInMemoryDbContext()
        {
            DbContextOptions<CRMDbContext> options = new DbContextOptionsBuilder<CRMDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            Mock<ICurrentUserService> mockUserService = new();
            mockUserService.Setup(u => u.UserId).Returns((string?) null); // Setup as system for syncing
            mockUserService.Setup(u => u.UserName).Returns("System");

            CRMDbContext context = new(options, mockUserService.Object);
            context.Database.EnsureCreated();
            return context;
        }

        [Fact]
        public async Task SyncUserAsync_NewUser_ShouldCreateUser()
        {
            // Arrange
            using CRMDbContext context = GetInMemoryDbContext();
            UserSynchronizationService service = new(context);

            List<Claim> claims = new()
            {
                new Claim(ClaimTypes.NameIdentifier, "sub123"),
                new Claim(ClaimTypes.Email, "test@example.com"),
                new Claim(ClaimTypes.Name, "Test User")
            };
            ClaimsIdentity identity = new(claims, "TestAuth");
            ClaimsPrincipal principal = new(identity);

            // Act
            await service.SyncUserAsync(principal);

            // Assert
            Core.Models.User? user = await context.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.SubjectId == "sub123");
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
            using CRMDbContext context = GetInMemoryDbContext();
            UserSynchronizationService service = new(context);

            // Pre-create user
            Core.Models.User existingUser = new()
            {
                SubjectId = "sub456",
                Email = "old@example.com",
                DisplayName = "Old Name",
                UserId = "System"
            };
            context.Users.Add(existingUser);
            await context.SaveChangesAsync();

            List<Claim> claims = new()
            {
                new Claim(ClaimTypes.NameIdentifier, "sub456"),
                new Claim(ClaimTypes.Email, "new@example.com"),
                new Claim(ClaimTypes.Name, "New Name")
            };
            ClaimsIdentity identity = new(claims, "TestAuth");
            ClaimsPrincipal principal = new(identity);

            // Act
            await service.SyncUserAsync(principal);

            // Assert
            Core.Models.User? user = await context.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.SubjectId == "sub456");
            Assert.NotNull(user);
            Assert.Equal("new@example.com", user.Email); // Should update
            Assert.Equal("New Name", user.DisplayName); // Should update

            // Check Principal modification
            Assert.DoesNotContain(principal.Claims, c => c.Type == ClaimTypes.NameIdentifier && c.Value == "sub456");
            Assert.Contains(principal.Claims, c => c.Type == ClaimTypes.NameIdentifier && c.Value == existingUser.Id.ToString());
        }
    }
}
