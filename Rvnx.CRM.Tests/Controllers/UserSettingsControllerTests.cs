using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Hosting;
using Moq;
using Rvnx.CRM.Core.DTOs.ApiToken;
using Rvnx.CRM.Core.DTOs.Immich;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models;
using Rvnx.CRM.Web.Controllers;
using Rvnx.CRM.Web.ViewModels.UserSettings;

namespace Rvnx.CRM.Tests.Controllers;

public class UserSettingsControllerTests : IDisposable
{
    private readonly Mock<ICurrentUserService> _currentUserMock;
    private readonly Mock<IApiTokenService> _apiTokenMock;
    private readonly Mock<IImmichSettingsService> _immichSettingsMock;
    private readonly Mock<IHostEnvironment> _hostEnvironmentMock;
    private readonly UserSettingsController _controller;
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _groupId = Guid.NewGuid();

    public UserSettingsControllerTests()
    {
        _currentUserMock = new Mock<ICurrentUserService>();
        _currentUserMock.Setup(s => s.UserId).Returns(_userId);
        _currentUserMock.Setup(s => s.GroupId).Returns(_groupId);
        _currentUserMock.Setup(s => s.DisplayName).Returns("Test User");
        _currentUserMock.Setup(s => s.Email).Returns("test@example.com");

        _apiTokenMock = new Mock<IApiTokenService>();

        _immichSettingsMock = new Mock<IImmichSettingsService>();
        _immichSettingsMock.Setup(s => s.ServerEnabled).Returns(true);
        _immichSettingsMock.Setup(s => s.GetSettingsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((ImmichSettingsDto?)null);

        _hostEnvironmentMock = new Mock<IHostEnvironment>();
        _hostEnvironmentMock.Setup(e => e.EnvironmentName).Returns("Production");

        _controller = new UserSettingsController(_currentUserMock.Object, _apiTokenMock.Object, _immichSettingsMock.Object, _hostEnvironmentMock.Object)
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
    public async Task IndexReturnsViewModelWithProfileAndActiveTokensOnly()
    {
        List<ApiToken> tokens =
        [
            new ApiToken { Id = Guid.NewGuid(), Name = "Active", TokenPrefix = "crm_aaaa", RevokedAt = null, ExpiresAt = null },
            new ApiToken { Id = Guid.NewGuid(), Name = "Revoked", TokenPrefix = "crm_bbbb", RevokedAt = DateTime.UtcNow.AddDays(-1) },
            new ApiToken { Id = Guid.NewGuid(), Name = "Expired", TokenPrefix = "crm_cccc", ExpiresAt = DateTime.UtcNow.AddMinutes(-1) }
        ];
        _apiTokenMock.Setup(s => s.ListTokensAsync(_userId)).ReturnsAsync(tokens);

        IActionResult result = await _controller.Index();

        ViewResult view = Assert.IsType<ViewResult>(result);
        UserSettingsViewModel model = Assert.IsType<UserSettingsViewModel>(view.Model);
        Assert.Equal("Test User", model.DisplayName);
        Assert.Equal("test@example.com", model.Email);
        Assert.Single(model.Tokens);
        Assert.Equal("Active", model.Tokens[0].Name);
    }

    [Fact]
    public async Task CreateApiTokenStoresRawTokenInTempDataAndRedirects()
    {
        CreateApiTokenFormDto form = new() { Name = "My Token", ExpiresAt = null };
        string rawToken = "crm_rawtokenvalue123";
        ApiToken created = new() { Id = Guid.NewGuid(), Name = form.Name, TokenPrefix = "crm_rawt" };

        _apiTokenMock.Setup(s => s.CreateTokenAsync(_userId, _groupId, form.Name, form.ExpiresAt))
            .ReturnsAsync((created, rawToken));

        IActionResult result = await _controller.CreateApiToken(form);

        RedirectToActionResult redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Equal(rawToken, _controller.TempData["NewApiToken"]);
        Assert.Equal(form.Name, _controller.TempData["NewApiTokenName"]);
    }

    [Fact]
    public async Task RevokeApiTokenCallsServiceAndRedirects()
    {
        Guid tokenId = Guid.NewGuid();

        IActionResult result = await _controller.RevokeApiToken(tokenId);

        RedirectToActionResult redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        _apiTokenMock.Verify(s => s.RevokeTokenAsync(tokenId, _userId), Times.Once);
    }

    [Fact]
    public async Task RevokeApiTokenClearsPendingNewTokenBanner()
    {
        _controller.TempData["NewApiToken"] = "crm_leftover";
        _controller.TempData["NewApiTokenName"] = "Leftover";

        await _controller.RevokeApiToken(Guid.NewGuid());

        Assert.False(_controller.TempData.ContainsKey("NewApiToken"));
        Assert.False(_controller.TempData.ContainsKey("NewApiTokenName"));
    }

    [Fact]
    public async Task IndexShowsDangerZoneAndDevOperationsWhenAdminInDevelopment()
    {
        _hostEnvironmentMock.Setup(e => e.EnvironmentName).Returns("Development");
        _currentUserMock.Setup(s => s.IsAdministratorAsync(_userId)).ReturnsAsync(true);

        IActionResult result = await _controller.Index();

        UserSettingsViewModel model = Assert.IsType<UserSettingsViewModel>(Assert.IsType<ViewResult>(result).Model);
        Assert.True(model.ShowDangerZone);
        Assert.True(model.ShowDevOperations);
    }

    [Fact]
    public async Task IndexShowsDangerZoneWithoutDevOperationsWhenAdminInProduction()
    {
        _hostEnvironmentMock.Setup(e => e.EnvironmentName).Returns("Production");
        _currentUserMock.Setup(s => s.IsAdministratorAsync(_userId)).ReturnsAsync(true);

        IActionResult result = await _controller.Index();

        UserSettingsViewModel model = Assert.IsType<UserSettingsViewModel>(Assert.IsType<ViewResult>(result).Model);
        Assert.True(model.ShowDangerZone);
        Assert.False(model.ShowDevOperations);
    }

    [Fact]
    public async Task IndexHidesDangerZoneWhenNotAdmin()
    {
        _hostEnvironmentMock.Setup(e => e.EnvironmentName).Returns("Development");
        _currentUserMock.Setup(s => s.IsAdministratorAsync(_userId)).ReturnsAsync(false);

        IActionResult result = await _controller.Index();

        UserSettingsViewModel model = Assert.IsType<UserSettingsViewModel>(Assert.IsType<ViewResult>(result).Model);
        Assert.False(model.ShowDangerZone);
        Assert.False(model.ShowDevOperations);
    }

    [Fact]
    public async Task IndexPrefillsImmichFormFromStoredSettings()
    {
        _immichSettingsMock.Setup(s => s.GetSettingsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ImmichSettingsDto { Enabled = true, BaseUrl = "https://immich.example.com/api", ApiKeyHint = "••••3kfa" });

        IActionResult result = await _controller.Index();

        UserSettingsViewModel model = Assert.IsType<UserSettingsViewModel>(Assert.IsType<ViewResult>(result).Model);
        Assert.True(model.HasImmichSettings);
        Assert.True(model.ImmichServerEnabled);
        Assert.True(model.ImmichForm.Enabled);
        Assert.Equal("https://immich.example.com/api", model.ImmichForm.BaseUrl);
        Assert.Null(model.ImmichForm.ApiKey);
    }

    [Fact]
    public async Task SaveImmichRedirectsOnSuccess()
    {
        _immichSettingsMock.Setup(s => s.SaveAsync(true, "https://immich.example.com/api", "key", It.IsAny<CancellationToken>()))
            .ReturnsAsync(ImmichSettingsOperationResult.Ok());

        IActionResult result = await _controller.SaveImmich(new ImmichSettingsFormDto
        {
            Enabled = true,
            BaseUrl = "https://immich.example.com/api",
            ApiKey = "key",
        });

        RedirectToActionResult redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(UserSettingsController.Index), redirect.ActionName);
        Assert.Equal("Immich settings saved.", _controller.TempData["ImmichSettings:Message"]);
    }

    [Fact]
    public async Task SaveImmichReturnsViewWithErrorsWhenServiceFails()
    {
        _immichSettingsMock.Setup(s => s.SaveAsync(It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ImmichSettingsOperationResult.Failure("An API key is required when connecting Immich for the first time."));

        IActionResult result = await _controller.SaveImmich(new ImmichSettingsFormDto
        {
            Enabled = true,
            BaseUrl = "https://immich.example.com/api",
        });

        ViewResult view = Assert.IsType<ViewResult>(result);
        Assert.Equal(nameof(UserSettingsController.Index), view.ViewName);
        Assert.False(_controller.ModelState.IsValid);
    }

    [Fact]
    public async Task SaveImmichSkipsServiceWhenModelInvalid()
    {
        _controller.ModelState.AddModelError("BaseUrl", "Server URL is required.");

        IActionResult result = await _controller.SaveImmich(new ImmichSettingsFormDto());

        Assert.IsType<ViewResult>(result);
        _immichSettingsMock.Verify(s => s.SaveAsync(It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SaveImmichForbiddenWhenServerDisabled()
    {
        _immichSettingsMock.Setup(s => s.ServerEnabled).Returns(false);

        IActionResult result = await _controller.SaveImmich(new ImmichSettingsFormDto
        {
            Enabled = true,
            BaseUrl = "https://immich.example.com/api",
            ApiKey = "key",
        });

        Assert.IsType<ForbidResult>(result);
        _immichSettingsMock.Verify(s => s.SaveAsync(It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteImmichRedirectsToIndex()
    {
        _immichSettingsMock.Setup(s => s.DeleteAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ImmichSettingsOperationResult.Ok());

        IActionResult result = await _controller.DeleteImmich();

        RedirectToActionResult redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(UserSettingsController.Index), redirect.ActionName);
        _immichSettingsMock.Verify(s => s.DeleteAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
