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
using Microsoft.AspNetCore.Http;
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
            mockCurrentUserService.Setup(s => s.UserId).Returns(Guid.Parse("c5b50a20-34b2-44b2-8b9c-aa4135f60938"));
            mockCurrentUserService.Setup(s => s.UserName).Returns("test-user");

            return new CRMDbContext(options, mockCurrentUserService.Object);
        }

        private static readonly Guid DefaultTestUserId = Guid.Parse("c5b50a20-34b2-44b2-8b9c-aa4135f60938");

        private ContactsController CreateController(CRMDbContext context, Guid? userId = null, bool isAuthenticated = true)
        {
            Guid resolvedUserId = userId ?? DefaultTestUserId;
            Repository repository = new(context);
            Mock<ILogger<ContactsController>> loggerMock = new();
            Mock<ICurrentUserService> userMock = new();
            userMock.Setup(u => u.UserId).Returns(resolvedUserId);
            userMock.Setup(u => u.UserName).Returns("Test User");
            userMock.Setup(u => u.IsAuthenticated).Returns(isAuthenticated);

            Mock<IUserSynchronizationService> syncMock = new();
            // Setup sync to do nothing by default, or verify it is called
            syncMock.Setup(s => s.SyncUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>())).Returns(Task.CompletedTask);

            ContactsController controller = new ContactsController(repository, loggerMock.Object, userMock.Object, new Mock<IVCardService>().Object, new Mock<IFileValidationService>().Object, syncMock.Object);

            // Need to set ControllerContext for HttpContext access (SyncUserAsync accesses it via passed principal, but controller passes HttpContext.User)
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };

            return controller;
        }

        [Fact]
        public async Task CreateSelf_Get_ShouldPreFillData()
        {
            // Arrange
            using CRMDbContext context = GetInMemoryDbContext();
            ContactsController controller = CreateController(context, userId: Guid.Parse("c5b50a20-34b2-44b2-8b9c-aa4135f60938"), isAuthenticated: true);

            User user = new() { Id = Guid.NewGuid(), SubjectId = "c5b50a20-34b2-44b2-8b9c-aa4135f60938", Email = "test@example.com" };
            context.Set<User>().Add(user);
            await context.SaveChangesAsync();

            // Act
            IActionResult result = await controller.CreateSelf();

            // Assert
            ViewResult viewResult = Assert.IsType<ViewResult>(result);
            CreateContactDto model = Assert.IsType<CreateContactDto>(viewResult.Model);
            Assert.Equal("test@example.com", model.Email);
            Assert.Equal("Test", model.FirstName);
            Assert.Equal("User", model.LastName);
            Assert.True(viewResult.ViewData["IsSelfCreate"] as bool?);
        }

        [Fact]
        public async Task CreateSelf_Get_ShouldSplitName_IntoFirstAndLast()
        {
            // Arrange
            using CRMDbContext context = GetInMemoryDbContext();
            Repository repository = new(context);
            Mock<ILogger<ContactsController>> loggerMock = new();
            Mock<ICurrentUserService> userMock = new();
            userMock.Setup(u => u.UserId).Returns(Guid.Parse("c5b50a20-34b2-44b2-8b9c-aa4135f60938"));
            userMock.Setup(u => u.UserName).Returns("Graham Sanders");
            userMock.Setup(u => u.IsAuthenticated).Returns(true);

            Mock<IUserSynchronizationService> syncMock = new();
            syncMock.Setup(s => s.SyncUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>())).Returns(Task.CompletedTask);

            ContactsController controller = new ContactsController(repository, loggerMock.Object, userMock.Object, new Mock<IVCardService>().Object, new Mock<IFileValidationService>().Object, syncMock.Object);
            controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };

            User user = new() { Id = Guid.NewGuid(), SubjectId = "c5b50a20-34b2-44b2-8b9c-aa4135f60938", Email = "test@example.com" };
            context.Set<User>().Add(user);
            await context.SaveChangesAsync();

            // Act
            IActionResult result = await controller.CreateSelf();

            // Assert
            ViewResult viewResult = Assert.IsType<ViewResult>(result);
            CreateContactDto model = Assert.IsType<CreateContactDto>(viewResult.Model);
            Assert.Equal("Graham", model.FirstName);
            Assert.Equal("Sanders", model.LastName);
        }

        [Fact]
        public async Task Self_ShouldRedirectToDetails_WhenSelfContactExists()
        {
            // Arrange
            using CRMDbContext context = GetInMemoryDbContext();
            ContactsController controller = CreateController(context);

            Contact contact = new() { Id = Guid.NewGuid(), FirstName = "Me" };
            User user = new() { Id = Guid.NewGuid(), SubjectId = "c5b50a20-34b2-44b2-8b9c-aa4135f60938", Email = "test@example.com", SelfContactId = contact.Id };

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

            User user = new() { Id = Guid.NewGuid(), SubjectId = "c5b50a20-34b2-44b2-8b9c-aa4135f60938", Email = "test@example.com", SelfContactId = null };
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
            User user = new() { Id = userId, SubjectId = "c5b50a20-34b2-44b2-8b9c-aa4135f60938", Email = "test@example.com" };
            context.Set<User>().Add(user);
            await context.SaveChangesAsync();

            CreateContactDto dto = new() { FirstName = "My Self", Email = "myself@example.com" };

            // Act
            IActionResult result = await controller.CreateSelf(dto);

            // Assert
            RedirectToActionResult redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Details", redirectResult.ActionName);

            User? updatedUser = await context.Set<User>().FirstOrDefaultAsync(u => u.Id == userId);
            Assert.NotNull(updatedUser);
            Assert.NotNull(updatedUser.SelfContactId);

            Contact? createdContact = await context.Set<Contact>().FirstOrDefaultAsync(c => c.Id == updatedUser.SelfContactId);
            Assert.NotNull(createdContact);
            Assert.Equal("My Self", createdContact.FirstName);
        }

        [Fact]
        public async Task CreateSelf_Post_ShouldNotCreateDuplicate_WhenExists()
        {
            // Arrange
            using CRMDbContext context = GetInMemoryDbContext();
            ContactsController controller = CreateController(context);

            Contact existingContact = new() { Id = Guid.NewGuid(), FirstName = "Existing" };
            User user = new() { Id = Guid.NewGuid(), SubjectId = "c5b50a20-34b2-44b2-8b9c-aa4135f60938", Email = "test@example.com", SelfContactId = existingContact.Id };

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

            int count = await context.Set<Contact>().CountAsync();
            Assert.Equal(1, count);
        }

        [Fact]
        public async Task Delete_ShouldUnlinkSelfContact_WhenContactIsDeleted()
        {
            // Arrange
            using CRMDbContext context = GetInMemoryDbContext();
            ContactsController controller = CreateController(context);

            Contact contact = new() { Id = Guid.NewGuid(), FirstName = "Me" };
            User user = new() { Id = Guid.NewGuid(), SubjectId = "c5b50a20-34b2-44b2-8b9c-aa4135f60938", Email = "test@example.com", SelfContactId = contact.Id };

            context.Set<User>().Add(user);
            context.Set<Contact>().Add(contact);
            await context.SaveChangesAsync();

            // Act
            // Note: DeleteConfirmed is a POST action
            IActionResult result = await controller.DeleteConfirmed(contact.Id);

            // Assert
            RedirectToActionResult redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);

            Contact? deletedContact = await context.Set<Contact>().FirstOrDefaultAsync(c => c.Id == contact.Id);
            Assert.Null(deletedContact);

            User? updatedUser = await context.Set<User>().FirstOrDefaultAsync(u => u.Id == user.Id);
            Assert.NotNull(updatedUser);
            Assert.Null(updatedUser.SelfContactId);
        }
    }
}