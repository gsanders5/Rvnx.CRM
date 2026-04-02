using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Rvnx.CRM.Core.DTOs.Contact;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Web.Controllers;
using System.Security.Claims;

namespace Rvnx.CRM.Tests.Services;

public class SelfContactTests
{
    private static readonly Guid DefaultTestUserId = Guid.Parse("c5b50a20-34b2-44b2-8b9c-aa4135f60938");

    private static ContactsController CreateController(Mock<ISelfContactService> selfContactServiceMock)
    {
        Mock<ILogger<ContactsController>> loggerMock = new();
        Mock<ICurrentUserService> userMock = new();
        userMock.Setup(u => u.UserId).Returns(DefaultTestUserId);
        userMock.Setup(u => u.UserName).Returns("Test User");
        userMock.Setup(u => u.IsAuthenticated).Returns(true);

        Mock<IUserSynchronizationService> syncMock = new();
        syncMock.Setup(s => s.SyncUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>())).Returns(Task.CompletedTask);

        ContactsController controller = new(loggerMock.Object, userMock.Object, syncMock.Object, new Mock<IContactImportService>().Object, new Mock<IContactExportService>().Object, new Mock<IContactManagementService>().Object, new Mock<IContactReadService>().Object, selfContactServiceMock.Object, Mock.Of<IFileValidationService>())
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        return controller;
    }

    [Fact]
    public async Task CreateSelfGetShouldPreFillData()
    {
        Mock<ISelfContactService> selfContactMock = new();
        selfContactMock.Setup(s => s.GetSelfContactIdAsync()).ReturnsAsync((Guid?)null);

        ContactFormDto formDto = new() { FirstName = "Test", LastName = "User", Email = "test@example.com" };
        selfContactMock.Setup(s => s.GetSelfContactFormAsync()).ReturnsAsync(formDto);

        ContactsController controller = CreateController(selfContactMock);

        IActionResult result = await controller.CreateSelf();

        ViewResult viewResult = Assert.IsType<ViewResult>(result);
        ContactCreateViewModel model = Assert.IsType<ContactCreateViewModel>(viewResult.Model);
        Assert.Equal("test@example.com", model.Email);
        Assert.Equal("Test", model.FirstName);
        Assert.True(model.IsSelfCreate);
    }

    [Fact]
    public async Task SelfShouldRedirectToDetailsWhenSelfContactExists()
    {
        Guid selfContactId = Guid.NewGuid();
        Mock<ISelfContactService> selfContactMock = new();
        selfContactMock.Setup(s => s.GetSelfContactIdAsync()).ReturnsAsync(selfContactId);

        ContactsController controller = CreateController(selfContactMock);

        IActionResult result = await controller.Self();

        RedirectToActionResult redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Details", redirectResult.ActionName);
        Assert.Equal(selfContactId, redirectResult.RouteValues?["id"]);
    }

    [Fact]
    public async Task SelfShouldRedirectToCreateSelfWhenSelfContactDoesNotExist()
    {
        Mock<ISelfContactService> selfContactMock = new();
        selfContactMock.Setup(s => s.GetSelfContactIdAsync()).ReturnsAsync((Guid?)null);

        ContactsController controller = CreateController(selfContactMock);

        IActionResult result = await controller.Self();

        RedirectToActionResult redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("CreateSelf", redirectResult.ActionName);
    }

    [Fact]
    public async Task CreateSelfPostShouldCreateContactAndLinkUser()
    {
        Guid newContactId = Guid.NewGuid();
        Mock<ISelfContactService> selfContactMock = new();

        selfContactMock.Setup(s => s.CreateSelfContactAsync(It.IsAny<ContactFormDto>()))
            .ReturnsAsync(ContactOperationResult.Ok(newContactId));

        ContactsController controller = CreateController(selfContactMock);
        ContactCreateViewModel dto = new() { FirstName = "My Self", Email = "myself@example.com" };

        IActionResult result = await controller.CreateSelf(dto);

        RedirectToActionResult redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Details", redirectResult.ActionName);
        Assert.Equal(newContactId, redirectResult.RouteValues?["id"]);
    }
}