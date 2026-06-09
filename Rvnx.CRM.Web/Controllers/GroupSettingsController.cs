using Microsoft.AspNetCore.Mvc;
using Rvnx.CRM.Core.DTOs.Immich;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Web.Constants;
using Rvnx.CRM.Web.Controllers.Base;
using Rvnx.CRM.Web.ViewModels.GroupSettings;

namespace Rvnx.CRM.Web.Controllers;

/// <summary>
/// Settings shared by every member of the current user's group, such as the
/// group's Immich server connection.
/// </summary>
public class GroupSettingsController(IImmichSettingsService immichSettingsService) : AuthorizedController
{
    private readonly IImmichSettingsService _immichSettingsService = immichSettingsService;

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        return View(await BuildViewModelAsync());
    }

    [HttpPost]
    public async Task<IActionResult> SaveImmich(ImmichSettingsFormDto form)
    {
        if (!ModelState.IsValid)
        {
            return View(nameof(Index), await BuildViewModelAsync(form));
        }

        ImmichSettingsOperationResult result = await _immichSettingsService.SaveAsync(form.Enabled, form.BaseUrl, form.ApiKey);
        if (!result.Success)
        {
            foreach (string error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error);
            }
            return View(nameof(Index), await BuildViewModelAsync(form));
        }

        TempData[TempDataKeys.GroupSettingsMessage] = "Immich settings saved.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> DeleteImmich()
    {
        ImmichSettingsOperationResult result = await _immichSettingsService.DeleteAsync();
        TempData[TempDataKeys.GroupSettingsMessage] = result.Success
            ? "Immich settings removed."
            : "There were no Immich settings to remove.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<GroupSettingsViewModel> BuildViewModelAsync(ImmichSettingsFormDto? submittedForm = null)
    {
        ImmichSettingsDto? immich = await _immichSettingsService.GetSettingsAsync();
        return new GroupSettingsViewModel
        {
            Immich = immich,
            ImmichForm = submittedForm ?? new ImmichSettingsFormDto
            {
                Enabled = immich?.Enabled ?? false,
                BaseUrl = immich?.BaseUrl ?? string.Empty,
            },
            StatusMessage = TempData[TempDataKeys.GroupSettingsMessage] as string,
        };
    }
}
