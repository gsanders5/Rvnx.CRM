using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using Rvnx.CRM.Core.DTOs.DebugOperations;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Web.Controllers;
using Rvnx.CRM.Web.ViewModels.DebugOperations;

namespace Rvnx.CRM.Tests.Controllers;

public class DebugOperationsControllerTests : IDisposable
{
    private readonly Mock<IDebugDataService> _debugDataServiceMock = new();
    private readonly Mock<IDebugOperationsService> _debugOperationsServiceMock = new();
    private readonly DebugOperationsController _controller;

    public DebugOperationsControllerTests()
    {
        _controller = new DebugOperationsController(_debugDataServiceMock.Object, _debugOperationsServiceMock.Object)
        {
            TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
        };
    }

    public void Dispose()
    {
        _controller.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task SeedTestDataRedirectsToUserSettings()
    {
        IActionResult result = await _controller.SeedTestData();

        RedirectToActionResult redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Equal("UserSettings", redirect.ControllerName);
        _debugDataServiceMock.Verify(s => s.SeedTestDataAsync(10), Times.Once);
    }

    [Fact]
    public async Task ResetDatabaseRedirectsToUserSettings()
    {
        IActionResult result = await _controller.ResetDatabase();

        RedirectToActionResult redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("UserSettings", redirect.ControllerName);
        _debugDataServiceMock.Verify(s => s.ResetDatabaseAsync(), Times.Once);
    }

    [Fact]
    public async Task MergeAccountsPostRejectsInvalidConfirmation()
    {
        IActionResult result = await _controller.MergeAccounts(Guid.NewGuid(), Guid.NewGuid(), "NOT_MERGE");

        RedirectToActionResult redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(DebugOperationsController.MergeAccounts), redirect.ActionName);
        _debugOperationsServiceMock.Verify(s => s.MergeAccountsAsync(It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task MergeAccountsGetReturnsViewWithUsers()
    {
        List<MergeUserDto> users = [new() { Id = Guid.NewGuid(), Name = "User 1", GroupName = "G", GroupMemberCount = 1 }];
        _debugOperationsServiceMock.Setup(s => s.GetAllUsersWithGroupsAsync()).ReturnsAsync(users);

        IActionResult result = await _controller.MergeAccounts();

        ViewResult view = Assert.IsType<ViewResult>(result);
        MergeAccountsViewModel model = Assert.IsType<MergeAccountsViewModel>(view.Model);
        Assert.Same(users, model.Users);
    }
}
