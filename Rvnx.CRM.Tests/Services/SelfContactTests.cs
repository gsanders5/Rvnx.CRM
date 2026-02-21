using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Rvnx.CRM.Core.DTOs.Contact;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Web.Controllers;
using System.Security.Claims;

namespace Rvnx.CRM.Tests.Services
{
    public class SelfContactTests
    {
        private static readonly Guid DefaultTestUserId = Guid.Parse("c5b50a20-34b2-44b2-8b9c-aa4135f60938");

        private ContactsController CreateController(Mock<ISelfContactService> selfContactServiceMock)
        {
            Mock<IRepository> repositoryMock = new();
            Mock<ILogger<ContactsController>> loggerMock = new();
            Mock<ICurrentUserService> userMock = new();
            userMock.Setup(u => u.UserId).Returns(DefaultTestUserId);
            userMock.Setup(u => u.UserName).Returns("Test User");
            userMock.Setup(u => u.IsAuthenticated).Returns(true);

            Mock<IUserSynchronizationService> syncMock = new();
            syncMock.Setup(s => s.SyncUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>())).Returns(Task.CompletedTask);

            ContactsController controller = new(repositoryMock.Object, loggerMock.Object, userMock.Object, new Mock<IContactImportService>().Object, new Mock<IContactExportService>().Object, new Mock<IContactManagementService>().Object, new Mock<IContactReadService>().Object, selfContactServiceMock.Object);

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
            Mock<ISelfContactService> selfContactMock = new();
            selfContactMock.Setup(s => s.GetSelfContactIdAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync((Guid?) null);

            ContactFormDto formDto = new() { FirstName = "Test", LastName = "User", Email = "test@example.com" };
            selfContactMock.Setup(s => s.GetSelfContactFormAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(formDto);

            ContactsController controller = CreateController(selfContactMock);

            // Act
            IActionResult result = await controller.CreateSelf();

            // Assert
            ViewResult viewResult = Assert.IsType<ViewResult>(result);
            ContactCreateViewModel model = Assert.IsType<ContactCreateViewModel>(viewResult.Model);
            Assert.Equal("test@example.com", model.Email);
            Assert.Equal("Test", model.FirstName);
            Assert.True(model.IsSelfCreate);
        }

        [Fact]
        public async Task Self_ShouldRedirectToDetails_WhenSelfContactExists()
        {
            // Arrange
            Guid selfContactId = Guid.NewGuid();
            Mock<ISelfContactService> selfContactMock = new();
            selfContactMock.Setup(s => s.GetSelfContactIdAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(selfContactId);

            ContactsController controller = CreateController(selfContactMock);

            // Act
            IActionResult result = await controller.Self();

            // Assert
            RedirectToActionResult redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Details", redirectResult.ActionName);
            Assert.Equal(selfContactId, redirectResult.RouteValues?["id"]);
        }

        [Fact]
        public async Task Self_ShouldRedirectToCreateSelf_WhenSelfContactDoesNotExist()
        {
            // Arrange
            Mock<ISelfContactService> selfContactMock = new();
            selfContactMock.Setup(s => s.GetSelfContactIdAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync((Guid?) null);

            ContactsController controller = CreateController(selfContactMock);

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
            Guid newContactId = Guid.NewGuid();
            Mock<ISelfContactService> selfContactMock = new();

            // Mock CreateSelfContactAsync success
            selfContactMock.Setup(s => s.CreateSelfContactAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<ContactFormDto>()))
                .ReturnsAsync(ContactOperationResult.Ok(newContactId));

            ContactsController controller = CreateController(selfContactMock);
            ContactCreateViewModel dto = new() { FirstName = "My Self", Email = "myself@example.com" };

            // Act
            IActionResult result = await controller.CreateSelf(dto);

            // Assert
            RedirectToActionResult redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Details", redirectResult.ActionName);
            Assert.Equal(newContactId, redirectResult.RouteValues?["id"]);
        }
    }
}
