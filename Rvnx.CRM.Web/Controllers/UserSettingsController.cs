using Microsoft.AspNetCore.Mvc;
using Rvnx.CRM.Core.DTOs.ApiToken;
using Rvnx.CRM.Core.Extensions;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models;
using Rvnx.CRM.Web.Constants;
using Rvnx.CRM.Web.Controllers.Base;
using Rvnx.CRM.Web.ViewModels.UserSettings;

namespace Rvnx.CRM.Web.Controllers;

public class UserSettingsController(
    ICurrentUserService currentUserService,
    IApiTokenService apiTokenService,
    IHostEnvironment hostEnvironment) : AuthorizedController
{
    private readonly ICurrentUserService _currentUserService = currentUserService;
    private readonly IApiTokenService _apiTokenService = apiTokenService;
    private readonly IHostEnvironment _hostEnvironment = hostEnvironment;

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        return View(await BuildViewModelAsync());
    }

    [HttpPost]
    public async Task<IActionResult> CreateApiToken(CreateApiTokenFormDto form)
    {
        Guid? userId = _currentUserService.UserId;
        Guid? groupId = _currentUserService.GroupId;
        if (userId == null || groupId == null)
        {
            return Forbid();
        }

        if (!ModelState.IsValid)
        {
            return View(nameof(Index), await BuildViewModelAsync());
        }

        (_, string rawToken) = await _apiTokenService.CreateTokenAsync(
            userId.Value, groupId.Value, form.Name, form.ExpiresAt);

        TempData[TempDataKeys.NewApiToken] = rawToken;
        TempData[TempDataKeys.NewApiTokenName] = form.Name;

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> RevokeApiToken(Guid id)
    {
        Guid? userId = _currentUserService.UserId;
        if (userId == null)
        {
            return Forbid();
        }

        await _apiTokenService.RevokeTokenAsync(id, userId.Value);
        TempData.Remove(TempDataKeys.NewApiToken);
        TempData.Remove(TempDataKeys.NewApiTokenName);
        return RedirectToAction(nameof(Index));
    }

    private async Task<UserSettingsViewModel> BuildViewModelAsync()
    {
        Guid? userId = _currentUserService.UserId;
        IEnumerable<ApiToken> tokens = userId == null
            ? []
            : await _apiTokenService.ListTokensAsync(userId.Value);

        bool isAdministrator = userId.HasValue
            && await _currentUserService.IsAdministratorAsync(userId.Value);

        return new UserSettingsViewModel
        {
            DisplayName = _currentUserService.DisplayName,
            Email = _currentUserService.Email,
            Tokens = [.. tokens.Where(t => t.IsActive).Select(t => t.ToDto())],
            ShowDangerZone = isAdministrator,
            IsDevelopment = _hostEnvironment.IsDevelopment(),
            NewlyCreatedRawToken = TempData[TempDataKeys.NewApiToken] as string,
            NewlyCreatedTokenName = TempData[TempDataKeys.NewApiTokenName] as string,
            DangerZoneMessage = TempData[TempDataKeys.DangerZoneMessage] as string,
            DangerZoneError = TempData[TempDataKeys.DangerZoneError] as string
        };
    }
}
