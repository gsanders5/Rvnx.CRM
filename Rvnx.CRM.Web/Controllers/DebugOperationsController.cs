using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Web.Controllers.Base;

namespace Rvnx.CRM.Web.Controllers
{
    public class DebugOperationsController(IDebugDataService debugDataService, IHostEnvironment environment) : AuthorizedController
    {
        private readonly IDebugDataService _debugDataService = debugDataService;
        private readonly IHostEnvironment _environment = environment;

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
    }
}
