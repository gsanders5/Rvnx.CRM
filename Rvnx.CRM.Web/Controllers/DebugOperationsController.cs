using Microsoft.AspNetCore.Mvc;
using Rvnx.CRM.Core.DTOs.DebugOperations;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Web.Constants;
using Rvnx.CRM.Web.Controllers.Base;
using Rvnx.CRM.Web.Filters;
using Rvnx.CRM.Web.ViewModels.DebugOperations;

namespace Rvnx.CRM.Web.Controllers;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CS8602:Dereference of a possibly null reference.",
    Justification = "Services injected via DI are guaranteed to be non-null.")]
[TypeFilter(typeof(RequireAdministratorFilter))]
public class DebugOperationsController(
    IDebugDataService debugDataService,
    IDebugOperationsService debugOperationsService) : AuthorizedController
{
    private readonly IDebugDataService _debugDataService = debugDataService;
    private readonly IDebugOperationsService _debugOperationsService = debugOperationsService;

    [HttpPost]
    [TypeFilter(typeof(RequireDevelopmentFilter), Order = -1)]
    public async Task<IActionResult> SeedTestData()
    {
        await _debugDataService.SeedTestDataAsync(10);
        return RedirectToAction("Index", "UserSettings");
    }

    [HttpPost]
    [TypeFilter(typeof(RequireDevelopmentFilter), Order = -1)]
    public async Task<IActionResult> ResetDatabase()
    {
        await _debugDataService.ResetDatabaseAsync();
        return RedirectToAction("Index", "UserSettings");
    }

    [HttpPost]
    [TypeFilter(typeof(RequireDevelopmentFilter), Order = -1)]
    public async Task<IActionResult> AddRandomRelationships()
    {
        int count = await _debugDataService.AddRandomRelationshipsAsync();

        TempData[TempDataKeys.DangerZoneMessage] = count == 0
            ? "Created 0 relationships (Check if contacts exist and types are defined)."
            : $"Created {count} relationships.";

        return RedirectToAction("Index", "UserSettings");
    }

    [HttpGet]
    public async Task<IActionResult> MergeAccounts()
    {
        List<MergeUserDto> users = await _debugOperationsService.GetAllUsersWithGroupsAsync();

        return View(new MergeAccountsViewModel { Users = users });
    }

    [HttpPost]
    public async Task<IActionResult> MergeAccounts(Guid user1Id, Guid user2Id, string confirmation)
    {
        if (confirmation != "MERGE")
        {
            TempData[TempDataKeys.ErrorMessage] = "Confirmation must be 'MERGE'.";
            return RedirectToAction(nameof(MergeAccounts));
        }

        MergeAccountsResult result = await _debugOperationsService.MergeAccountsAsync(user1Id, user2Id);

        if (!result.Success)
        {
            TempData[TempDataKeys.ErrorMessage] = result.Error ?? "An error occurred during merge.";
            return RedirectToAction(nameof(MergeAccounts));
        }

        TempData[TempDataKeys.SuccessMessage] = result.Message;
        return RedirectToAction(nameof(MergeAccounts));
    }
}
