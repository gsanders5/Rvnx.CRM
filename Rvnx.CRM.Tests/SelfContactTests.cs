using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Rvnx.CRM.Core.DTOs.Contact;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Infrastructure.Data;
using Rvnx.CRM.Infrastructure.Repositories;
using Rvnx.CRM.Web.Controllers;
using System;
using System.Threading.Tasks;
using Xunit;
using Rvnx.CRM.Core.Models;

namespace Rvnx.CRM.Tests
{
    public class SelfContactTests
    {
        private CRMDbContext GetInMemoryDbContext()
        {
            DbContextOptions<CRMDbContext> options = new DbContextOptionsBuilder<CRMDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            Mock<ICurrentUserService> mockCurrentUserService = new();
            mockCurrentUserService.Setup(s => s.UserId).Returns("test-user-id");
            mockCurrentUserService.Setup(s => s.UserName).Returns("test-user");

            return new CRMDbContext(options, mockCurrentUserService.Object);
        }

        private ContactsController CreateController(CRMDbContext context, string userId = "test-user-id", bool isAuthenticated = true)
        {
            Repository repository = new(context);
            Mock<ILogger<ContactsController>> loggerMock = new();
            Mock<ICurrentUserService> userMock = new();
            userMock.Setup(u => u.UserId).Returns(userId);
            userMock.Setup(u => u.UserName).Returns("Test User");
            userMock.Setup(u => u.IsAuthenticated).Returns(isAuthenticated);

            return new ContactsController(repository, loggerMock.Object, userMock.Object);
        }

        [Fact]
        public async Task Self_ShouldRedirectToDetails_WhenSelfContactExists()
        {
            // Arrange
            using CRMDbContext context = GetInMemoryDbContext();
            ContactsController controller = CreateController(context);

            Guid linkedUserId = Guid.NewGuid();
            User user = new() { Id = linkedUserId, SubjectId = "test-user-id", Email = "test@example.com" };
            Contact contact = new() { Id = Guid.NewGuid(), FirstName = "Me", LinkedUserId = linkedUserId };

            context.Set<User>().Add(user);
            context.Set<Contact>().Add(contact);
            await context.SaveChangesAsync();

            // Act
            IActionResult result = await controller.Self();

            // Assert
            RedirectToActionResult redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Details", redirectResult.ActionName);
            Assert.Equal(contact.Id, redirectResult.RouteValues?["id"]);
        }

        [Fact]
        public async Task Self_ShouldRedirectToCreateSelf_WhenSelfContactDoesNotExist()
        {
            // Arrange
            using CRMDbContext context = GetInMemoryDbContext();
            ContactsController controller = CreateController(context);

            User user = new() { Id = Guid.NewGuid(), SubjectId = "test-user-id", Email = "test@example.com" };
            context.Set<User>().Add(user);
            await context.SaveChangesAsync();

            // Act
            IActionResult result = await controller.Self();

            // Assert
            RedirectToActionResult redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("CreateSelf", redirectResult.ActionName);
        }

        [Fact]
        public async Task CreateSelf_Post_ShouldCreateContactAndLinkUser()
        {
            // Arrange
            using CRMDbContext context = GetInMemoryDbContext();
            ContactsController controller = CreateController(context);

            Guid userId = Guid.NewGuid();
            User user = new() { Id = userId, SubjectId = "test-user-id", Email = "test@example.com" };
            context.Set<User>().Add(user);
            await context.SaveChangesAsync();

            CreateContactDto dto = new() { FirstName = "My Self", Email = "myself@example.com" };

            // Act
            IActionResult result = await controller.CreateSelf(dto);

            // Assert
            RedirectToActionResult redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Details", redirectResult.ActionName);

            Contact? createdContact = await context.Set<Contact>().FirstOrDefaultAsync(c => c.LinkedUserId == userId);
            Assert.NotNull(createdContact);
            Assert.Equal("My Self", createdContact.FirstName);
            Assert.Equal(userId, createdContact.LinkedUserId);
        }

        [Fact]
        public async Task CreateSelf_Post_ShouldNotCreateDuplicate_WhenExists()
        {
            // Arrange
            using CRMDbContext context = GetInMemoryDbContext();
            ContactsController controller = CreateController(context);

            Guid userId = Guid.NewGuid();
            User user = new() { Id = userId, SubjectId = "test-user-id", Email = "test@example.com" };
            Contact existingContact = new() { Id = Guid.NewGuid(), FirstName = "Existing", LinkedUserId = userId };

            context.Set<User>().Add(user);
            context.Set<Contact>().Add(existingContact);
            await context.SaveChangesAsync();

            CreateContactDto dto = new() { FirstName = "New Self", Email = "new@example.com" };

            // Act
            IActionResult result = await controller.CreateSelf(dto);

            // Assert
            RedirectToActionResult redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Details", redirectResult.ActionName);
            Assert.Equal(existingContact.Id, redirectResult.RouteValues?["id"]);

            int count = await context.Set<Contact>().CountAsync(c => c.LinkedUserId == userId);
            Assert.Equal(1, count);
        }
    }
}
