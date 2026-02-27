using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Web.Controllers.Base;
using Rvnx.CRM.Web.Models;

#pragma warning disable CS8602 // Dereference of a possibly null reference

namespace Rvnx.CRM.Web.Controllers
{
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

            if (count == 0)
            {
                // We can't distinguish between "No types" and "Not enough contacts" easily without changing interface,
                // but for debug tool "Created 0" is acceptable or we can assume it failed if we expected some.
                // Or we can just say "Created 0 relationships."
                TempData["Message"] = "Created 0 relationships (Check if contacts exist and types are defined).";
            }
            else
            {
                TempData["Message"] = $"Created {count} relationships.";
            }

            return RedirectToAction("Index");
        }

        private async Task<bool> IsAdminAsync()
        {
            Guid? userId = _currentUserService.UserId;
            return userId != null && await _debugOperationsService.IsAdministratorAsync(userId.Value);
        }

        [HttpGet]
        public async Task<IActionResult> MergeAccounts()
        {
            if (!await IsAdminAsync())
            {
                return Forbid();
            }

            List<MergeUserDto> users = await _debugOperationsService.GetAllUsersWithGroupsAsync();

            return View(new MergeAccountsViewModel { Users = users });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MergeAccounts(Guid user1Id, Guid user2Id, string confirmation)
        {
            if (!await IsAdminAsync())
            {
                return Forbid();
            }

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
}
