using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Web.Controllers.Base;
using Rvnx.CRM.Web.Filters;
using Rvnx.CRM.Web.Models;

namespace Rvnx.CRM.Web.Controllers;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CS8602:Dereference of a possibly null reference.",
    Justification = "Services injected via DI are guaranteed to be non-null.")]
[TypeFilter(typeof(RequireAdministratorFilter))]
public class DebugOperationsController(
    IDebugDataService debugDataService,
    IDebugOperationsService debugOperationsService,
    IHostEnvironment environment,
    ICurrentUserService currentUserService) : AuthorizedController
{
    private readonly IDebugDataService _debugDataService = debugDataService;
    private readonly IDebugOperationsService _debugOperationsService = debugOperationsService;
    private readonly IHostEnvironment _environment = environment;
    private readonly ICurrentUserService _currentUserService = currentUserService;

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        if (!_environment.IsDevelopment())
        {
            context.Result = new NotFoundResult();
            return;
        }

        base.OnActionExecuting(context);
    }

    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SeedTestData()
    {
        await _debugDataService.SeedTestDataAsync(10);
        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetDatabase()
    {
        await _debugDataService.ResetDatabaseAsync();
        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddRandomRelationships()
    {
        int count = await _debugDataService.AddRandomRelationshipsAsync();

        TempData["Message"] = count == 0
            ? "Created 0 relationships (Check if contacts exist and types are defined)."
            : $"Created {count} relationships.";

        return RedirectToAction("Index");
    }

    [HttpGet]
    public async Task<IActionResult> MergeAccounts()
    {
        List<MergeUserDto> users = await _debugOperationsService.GetAllUsersWithGroupsAsync();

        return View(new MergeAccountsViewModel { Users = users });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MergeAccounts(Guid user1Id, Guid user2Id, string confirmation)
    {
        if (confirmation != "MERGE")
        {
            TempData["Error"] = "Confirmation must be 'MERGE'.";
            return RedirectToAction(nameof(MergeAccounts));
        }

        MergeAccountsResult result = await _debugOperationsService.MergeAccountsAsync(user1Id, user2Id);

        if (!result.Success)
        {
            TempData["Error"] = result.Error ?? "An error occurred during merge.";
            return RedirectToAction(nameof(MergeAccounts));
        }

        TempData["Message"] = result.Message;
        return RedirectToAction(nameof(MergeAccounts));
    }
}