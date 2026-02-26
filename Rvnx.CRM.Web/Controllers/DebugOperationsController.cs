using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models;
using Rvnx.CRM.Core.Models.Base;
using Rvnx.CRM.Infrastructure.Data;
using Rvnx.CRM.Web.Controllers.Base;
using Rvnx.CRM.Web.Models;

#pragma warning disable CS8602 // Dereference of a possibly null reference

namespace Rvnx.CRM.Web.Controllers
{
    public class DebugOperationsController(
        IDebugDataService debugDataService,
        IHostEnvironment environment,
        CRMDbContext context,
        ICurrentUserService currentUserService,
        ILogger<DebugOperationsController> logger) : AuthorizedController
    {
        private readonly IDebugDataService _debugDataService = debugDataService;
        private readonly IHostEnvironment _environment = environment;
        private readonly CRMDbContext _context = context;
        private readonly ICurrentUserService _currentUserService = currentUserService;
        private readonly ILogger<DebugOperationsController> _logger = logger;

        private static readonly Action<ILogger, Guid, Guid, Guid?, DateTime, Exception?> LogMerge =
            LoggerMessage.Define<Guid, Guid, Guid?, DateTime>(
                LogLevel.Information,
                new EventId(1, nameof(MergeAccounts)),
                "Merged group {DiscardedGroupId} into {KeptGroupId}. Administrator: {AdminId}. Timestamp: {Timestamp}");

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
            if (userId == null) return false;

            User? user = await _context.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == userId);
            return user?.IsAdministrator ?? false;
        }

        [HttpGet]
        public async Task<IActionResult> MergeAccounts()
        {
            if (!await IsAdminAsync()) return Forbid();

            List<MergeUserDto> users = await _context.Users.IgnoreQueryFilters()
                .Include(u => u.Group)
                    .ThenInclude(g => g.Members)
                .Select(u => new MergeUserDto
                {
                    Id = u!.Id,
                    Name = u!.DisplayName ?? u!.Email,
                    GroupName = u!.Group == null ? "No Group" : u!.Group!.Name,
                    GroupMemberCount = u!.Group == null ? 0 : u!.Group!.Members!.Count
                })
                .ToListAsync();

            return View(new MergeAccountsViewModel { Users = users });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MergeAccounts(Guid user1Id, Guid user2Id, string confirmation)
        {
            if (!await IsAdminAsync()) return Forbid();

            if (confirmation != "MERGE")
            {
                TempData["Error"] = "Confirmation must be 'MERGE'.";
                return RedirectToAction(nameof(MergeAccounts));
            }

            if (user1Id == user2Id)
            {
                TempData["Error"] = "Cannot merge same user.";
                return RedirectToAction(nameof(MergeAccounts));
            }

            User? user1 = await _context.Users.IgnoreQueryFilters().Include(u => u.Group).ThenInclude(g => g.Members).FirstOrDefaultAsync(u => u.Id == user1Id);
            User? user2 = await _context.Users.IgnoreQueryFilters().Include(u => u.Group).ThenInclude(g => g.Members).FirstOrDefaultAsync(u => u.Id == user2Id);

            if (user1 == null || user2 == null)
            {
                TempData["Error"] = "User not found.";
                return RedirectToAction(nameof(MergeAccounts));
            }

            UserGroup? group1 = user1.Group;
            UserGroup? group2 = user2.Group;

            if (group1 == null || group2 == null)
            {
                TempData["Error"] = "One or both users have no group.";
                return RedirectToAction(nameof(MergeAccounts));
            }

            if (group1.Id == group2.Id)
            {
                TempData["Message"] = "Users are already in the same group.";
                return RedirectToAction(nameof(MergeAccounts));
            }

            // Decide which group to keep
            // Prefer larger group
            UserGroup g1 = group1!;
            UserGroup g2 = group2!;

            UserGroup keptGroup = g1.Members!.Count >= g2.Members!.Count ? g1 : g2;
            UserGroup discardedGroup = keptGroup == g1 ? g2 : g1;

            // Move all entities
            Guid keptGroupId = keptGroup.Id;
            Guid discardedGroupId = discardedGroup.Id;

            // Update all filtered entities
            IEnumerable<Microsoft.EntityFrameworkCore.Metadata.IEntityType> entityTypes = _context.Model.GetEntityTypes()
                .Where(e => typeof(BaseEntity).IsAssignableFrom(e.ClrType) && !typeof(IGlobalEntity).IsAssignableFrom(e.ClrType));

            foreach (Microsoft.EntityFrameworkCore.Metadata.IEntityType? entityType in entityTypes)
            {
                string? tableName = entityType.GetTableName();
#pragma warning disable EF1002 // SQL Injection risk
                if (tableName != null)
                {
                    // In-memory provider doesn't support raw SQL for updates like this.
                    // For now, if not relational, we skip optimization and assume standard tracking (which might be slower but correct).
                    // Or check if provider is relational.
                    if (_context.Database.IsRelational())
                    {
                        await _context.Database.ExecuteSqlRawAsync($"UPDATE \"{tableName}\" SET \"GroupId\" = {{0}} WHERE \"GroupId\" = {{1}}", keptGroupId, discardedGroupId);
                    }
                    else
                    {
                        // Fallback for InMemory (primarily for tests)
                        // In memory, we must update entities via EF Core tracking.
                        // This is slow but necessary for tests to pass asserting that entities moved.
                        if (entityType.ClrType != null)
                        {
                            System.Reflection.MethodInfo? method = typeof(DebugOperationsController)
                                .GetMethod(nameof(UpdateGroupIdsInMemory), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                                ?.MakeGenericMethod(entityType.ClrType);

                            if (method != null)
                            {
                                await (Task)method.Invoke(this, [keptGroupId, discardedGroupId])!;
                            }
                        }
                    }
                }
#pragma warning restore EF1002
            }

            // Move users
            foreach (User? member in discardedGroup.Members.ToList())
            {
                member.GroupId = keptGroupId;
                member.Group = keptGroup; // Ensure navigation property is updated if tracked
            }

            // Delete discarded group
            _context.UserGroups.Remove(discardedGroup);

            await _context.SaveChangesAsync();

            LogMerge(_logger, discardedGroupId, keptGroupId, _currentUserService.UserId, DateTime.UtcNow, null);

            TempData["Message"] = $"Successfully merged users. Kept group: {keptGroup.Name} ({keptGroup.Members.Count} members).";

            return RedirectToAction(nameof(MergeAccounts));
        }

        private async Task UpdateGroupIdsInMemory<T>(Guid keptGroupId, Guid discardedGroupId) where T : BaseEntity
        {
            List<T> entities = await _context.Set<T>().IgnoreQueryFilters().Where(e => e.GroupId == discardedGroupId).ToListAsync();
            foreach (T entity in entities)
            {
                entity.GroupId = keptGroupId;
            }
        }
    }
}
