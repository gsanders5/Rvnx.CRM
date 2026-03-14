using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models;
using Rvnx.CRM.Core.Models.Base;
using Rvnx.CRM.Infrastructure.Data;

namespace Rvnx.CRM.Infrastructure.Services;

public class DebugOperationsService(
    CRMDbContext context,
    IRepository repository,
    ICurrentUserService currentUserService,
    ILogger<DebugOperationsService> logger) : IDebugOperationsService
{
    private readonly CRMDbContext _context = context;
    private readonly IRepository _repository = repository;
    private readonly ICurrentUserService _currentUserService = currentUserService;
    private readonly ILogger<DebugOperationsService> _logger = logger;

    private static readonly Action<ILogger, Guid, Guid, Guid?, DateTime, Exception?> LogMerge =
        LoggerMessage.Define<Guid, Guid, Guid?, DateTime>(
            LogLevel.Information,
            new EventId(1, nameof(MergeAccountsAsync)),
            "Merged group {DiscardedGroupId} into {KeptGroupId}. Administrator: {AdminId}. Timestamp: {Timestamp}");

    public async Task<List<MergeUserDto>> GetAllUsersWithGroupsAsync()
    {
        List<User> users = await _repository.QueryUnfiltered<User>()
            .Include(u => u.Group)
                .ThenInclude(g => g!.Members)
            .ToListAsync();

        return users.Select(u =>
        {
            string groupName = "No Group";
            int memberCount = 0;

            // Explicit null check for Group to avoid CS8602
            if (u.Group != null)
            {
                groupName = u.Group.Name;
                if (u.Group.Members != null)
                {
                    memberCount = u.Group.Members.Count;
                }
            }

            return new MergeUserDto
            {
                Id = u.Id,
                Name = u.DisplayName ?? u.Email ?? "Unknown User",
                GroupName = groupName,
                GroupMemberCount = memberCount
            };
        }).ToList();
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Security", "EF1002:Risk of vulnerability to SQL injection.", Justification = "tableName is sourced from EF Core's GetTableName() metadata, not user input, and parameters are passed safely.")]
    public async Task<MergeAccountsResult> MergeAccountsAsync(Guid user1Id, Guid user2Id)
    {
        if (user1Id == user2Id)
        {
            return new MergeAccountsResult { Success = false, Error = "Cannot merge same user." };
        }

        User? user1 = await _repository.QueryUnfiltered<User>()
            .Include(u => u.Group)
            .ThenInclude(g => g!.Members)
            .FirstOrDefaultAsync(u => u!.Id == user1Id);

        User? user2 = await _repository.QueryUnfiltered<User>()
            .Include(u => u.Group)
            .ThenInclude(g => g!.Members)
            .FirstOrDefaultAsync(u => u!.Id == user2Id);

        if (user1 == null || user2 == null)
        {
            return new MergeAccountsResult { Success = false, Error = "User not found." };
        }

        UserGroup? group1 = user1.Group;
        UserGroup? group2 = user2.Group;

        if (group1 == null || group2 == null)
        {
            return new MergeAccountsResult { Success = false, Error = "One or both users have no group." };
        }

        if (group1.Id == group2.Id)
        {
            return new MergeAccountsResult { Success = false, Message = "Users are already in the same group." };
        }

        UserGroup g1 = group1!;
        UserGroup g2 = group2!;

        int count1 = 0;
        // Suppress nullable warning as we checked for nulls above, but static analysis might not infer deep prop
        if (g1.Members != null)
        {
            count1 = g1.Members.Count;
        }

        int count2 = 0;
        if (g2.Members != null)
        {
            count2 = g2.Members.Count;
        }

        UserGroup keptGroup = count1 >= count2 ? g1 : g2;
        UserGroup discardedGroup = keptGroup.Id == g1.Id ? g2 : g1;

        Guid keptGroupId = keptGroup.Id;
        Guid discardedGroupId = discardedGroup.Id;

        IEnumerable<Microsoft.EntityFrameworkCore.Metadata.IEntityType> entityTypes = _context.Model.GetEntityTypes()
            .Where(e => typeof(BaseEntity).IsAssignableFrom(e.ClrType) && !typeof(IGlobalEntity).IsAssignableFrom(e.ClrType));

        foreach (Microsoft.EntityFrameworkCore.Metadata.IEntityType? entityType in entityTypes)
        {
            string? tableName = entityType.GetTableName();
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
                        System.Reflection.MethodInfo? method = typeof(DebugOperationsService)
                            .GetMethod(nameof(UpdateGroupIdsInMemory), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                            ?.MakeGenericMethod(entityType.ClrType);

                        if (method != null)
                        {
                            await (Task)method.Invoke(this, [keptGroupId, discardedGroupId])!;
                        }
                    }
                }
            }
        }

        if (discardedGroup.Members != null)
        {
            foreach (User? member in discardedGroup.Members.ToList())
            {
                member.GroupId = keptGroupId;
                member.Group = keptGroup; // Ensure navigation property is updated if tracked
            }
        }

        _context.UserGroups.Remove(discardedGroup);

        await _context.SaveChangesAsync();

        LogMerge(_logger, discardedGroupId, keptGroupId, _currentUserService.UserId, DateTime.UtcNow, null);

        return new MergeAccountsResult
        {
            Success = true,
            Message = $"Successfully merged users. Kept group: {keptGroup.Name} ({keptGroup.Members?.Count ?? 0} members)."
        };
    }

    private async Task UpdateGroupIdsInMemory<T>(Guid keptGroupId, Guid discardedGroupId) where T : BaseEntity
    {
        List<T> entities = await _repository.QueryUnfiltered<T>().Where(e => e.GroupId == discardedGroupId).ToListAsync();
        foreach (T entity in entities)
        {
            entity.GroupId = keptGroupId;
        }
    }
}