using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using Rvnx.CRM.Core.DTOs.Immich;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Web.Controllers;
using Rvnx.CRM.Web.ViewModels.GroupSettings;

namespace Rvnx.CRM.Tests.Controllers;

public sealed class GroupSettingsControllerTests : IDisposable
{
    private readonly Mock<IImmichSettingsService> _mockService = new();
    private readonly GroupSettingsController _controller;

    public GroupSettingsControllerTests()
    {
        _controller = new GroupSettingsController(_mockService.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() },
            TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>()),
        };
    }

    public void Dispose()
    {
        _controller.Dispose();
    }

    [Fact]
    public async Task IndexShowsEmptyFormWhenNothingStored()
    {
        _mockService.Setup(s => s.GetSettingsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((ImmichSettingsDto?)null);

        IActionResult result = await _controller.Index();

        ViewResult view = Assert.IsType<ViewResult>(result);
        GroupSettingsViewModel vm = Assert.IsType<GroupSettingsViewModel>(view.Model);
        Assert.False(vm.HasImmichSettings);
        Assert.Equal(string.Empty, vm.ImmichForm.BaseUrl);
    }

    [Fact]
    public async Task IndexPrefillsFormFromStoredSettings()
    {
        _mockService.Setup(s => s.GetSettingsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ImmichSettingsDto { Enabled = true, BaseUrl = "https://immich.example.com/api", ApiKeyHint = "••••3kfa" });

        IActionResult result = await _controller.Index();

        ViewResult view = Assert.IsType<ViewResult>(result);
        GroupSettingsViewModel vm = Assert.IsType<GroupSettingsViewModel>(view.Model);
        Assert.True(vm.HasImmichSettings);
        Assert.True(vm.ImmichForm.Enabled);
        Assert.Equal("https://immich.example.com/api", vm.ImmichForm.BaseUrl);
        Assert.Null(vm.ImmichForm.ApiKey);
    }

    [Fact]
    public async Task SaveImmichRedirectsOnSuccess()
    {
        _mockService.Setup(s => s.SaveAsync(true, "https://immich.example.com/api", "key", It.IsAny<CancellationToken>()))
            .ReturnsAsync(ImmichSettingsOperationResult.Ok(Guid.NewGuid()));

        IActionResult result = await _controller.SaveImmich(new ImmichSettingsFormDto
        {
            Enabled = true,
            BaseUrl = "https://immich.example.com/api",
            ApiKey = "key",
        });

        RedirectToActionResult redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(GroupSettingsController.Index), redirect.ActionName);
    }

    [Fact]
    public async Task SaveImmichReturnsViewWithErrorsWhenServiceFails()
    {
        _mockService.Setup(s => s.GetSettingsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((ImmichSettingsDto?)null);
        _mockService.Setup(s => s.SaveAsync(It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ImmichSettingsOperationResult.Failure("An API key is required when connecting Immich for the first time."));

        IActionResult result = await _controller.SaveImmich(new ImmichSettingsFormDto
        {
            Enabled = true,
            BaseUrl = "https://immich.example.com/api",
        });

        ViewResult view = Assert.IsType<ViewResult>(result);
        Assert.Equal(nameof(GroupSettingsController.Index), view.ViewName);
        Assert.False(_controller.ModelState.IsValid);
    }

    [Fact]
    public async Task SaveImmichSkipsServiceWhenModelInvalid()
    {
        _mockService.Setup(s => s.GetSettingsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((ImmichSettingsDto?)null);
        _controller.ModelState.AddModelError("BaseUrl", "Server URL is required.");

        IActionResult result = await _controller.SaveImmich(new ImmichSettingsFormDto());

        Assert.IsType<ViewResult>(result);
        _mockService.Verify(s => s.SaveAsync(It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteImmichRedirectsToIndex()
    {
        _mockService.Setup(s => s.DeleteAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ImmichSettingsOperationResult.Ok(Guid.NewGuid()));

        IActionResult result = await _controller.DeleteImmich();

        RedirectToActionResult redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(GroupSettingsController.Index), redirect.ActionName);
        _mockService.Verify(s => s.DeleteAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
