using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Rvnx.CRM.Core.Constants;
using Rvnx.CRM.Core.DTOs.Base;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models;
using Rvnx.CRM.Core.Models.Base;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Core.Models.Dates;
using Rvnx.CRM.Core.Services;
using Rvnx.CRM.Infrastructure.Data;

namespace Rvnx.CRM.Infrastructure.Services;

public class DebugDataService(IRepository repository, CRMDbContext context, ILogger<DebugDataService> logger) : IDebugDataService
{
    private readonly IRepository _repository = repository;
    private readonly CRMDbContext _context = context;
    private readonly ILogger<DebugDataService> _logger = logger;

    private static readonly Action<ILogger, Guid, Guid, Guid?, DateTime, Exception?> LogMerge =
        LoggerMessage.Define<Guid, Guid, Guid?, DateTime>(
            LogLevel.Information,
            new EventId(1, nameof(MergeAccountsAsync)),
            "Merged group {DiscardedGroupId} into {KeptGroupId}. Administrator: {AdminId}. Timestamp: {Timestamp}");

    public async Task SeedTestDataAsync(int count)
    {
        List<Contact> contacts = FakeDataGenerator.GenerateContacts(count);

        foreach (Contact contact in contacts)
        {
            if (contact.Id == Guid.Empty)
            {
                contact.Id = Guid.NewGuid();
            }

            List<Address>? addresses = contact.Addresses?.ToList();
            List<ContactMethod>? infos = contact.ContactMethods?.ToList();
            List<SignificantDate>? dates = contact.SignificantDates?.ToList();

            contact.Addresses = [];
            contact.ContactMethods = [];
            contact.SignificantDates = [];

            await _repository.AddAsync(contact);
            await _repository.SaveChangesAsync();

            if (addresses != null)
            {
                foreach (Address? addr in addresses)
                {
                    addr.ContactId = contact.Id;
                    await _repository.AddAsync(addr);
                }
            }

            if (infos != null)
            {
                foreach (ContactMethod? info in infos)
                {
                    info.ContactId = contact.Id;
                    await _repository.AddAsync(info);
                }
            }

            if (dates != null)
            {
                foreach (SignificantDate? date in dates)
                {
                    date.ContactId = contact.Id;
                    await _repository.AddAsync(date);
                }
            }
        }

        await _repository.SaveChangesAsync();
    }

    public async Task ResetDatabaseAsync()
    {
        List<Contact> contacts = await _repository.ListAsync<Contact>();
        await _repository.DeleteRangeAsync(contacts);

        List<Note> notes = await _repository.ListAsync<Note>();
        await _repository.DeleteRangeAsync(notes);

        List<Reminder> reminders = await _repository.ListAsync<Reminder>();
        await _repository.DeleteRangeAsync(reminders);

        List<SignificantDate> dates = await _repository.ListAsync<SignificantDate>();
        await _repository.DeleteRangeAsync(dates);

        List<ContactMethod> infos = await _repository.ListAsync<ContactMethod>();
        await _repository.DeleteRangeAsync(infos);

        List<Fact> facts = await _repository.ListAsync<Fact>();
        await _repository.DeleteRangeAsync(facts);

        List<Address> addresses = await _repository.ListAsync<Address>();
        await _repository.DeleteRangeAsync(addresses);

        List<Relationship> relationships = await _repository.ListAsync<Relationship>();
        await _repository.DeleteRangeAsync(relationships);

        await _repository.SaveChangesAsync();
    }

    public async Task<int> AddRandomRelationshipsAsync(int maxRelationships = 50)
    {
        // 1. Get available Relationship Types (Static)
        List<RelationshipTypeDefinition> types = (List<RelationshipTypeDefinition>)RelationshipTypeService.GetAll();
        if (types.Count == 0)
        {
            return 0;
        }

        // 2. Get Contacts
        List<Contact> contacts = await _repository.ListAsNoTrackingAsync<Contact>();
        if (contacts.Count < 2)
        {
            return 0;
        }

        // 3. Generate Random Relationships
        Random random = new();
        int relationshipsToCreate = Math.Min(contacts.Count * 2, maxRelationships);
        int createdCount = 0;

        for (int i = 0; i < relationshipsToCreate; i++)
        {
            Contact c1 = contacts[random.Next(contacts.Count)];
            Contact c2 = contacts[random.Next(contacts.Count)];

            if (c1.Id == c2.Id)
            {
                continue;
            }

            RelationshipTypeDefinition type = types[random.Next(types.Count)];

            List<Relationship> existing = await _repository.ListAsNoTrackingAsync<Relationship>(r =>
                r.EntityId == c1.Id && r.RelatedEntityId == c2.Id && r.RelationshipTypeId == type.Id);

            if (existing.Count > 0)
            {
                continue;
            }

            Relationship rel = new()
            {
                Id = Guid.NewGuid(),
                EntityId = c1.Id,
                RelatedEntityId = c2.Id,
                EntityType = EntityTypes.Person,
                RelationshipTypeId = type.Id,
                Description = "Randomly generated"
            };

            await _repository.AddAsync(rel);
            createdCount++;
        }

        await _repository.SaveChangesAsync();
        return createdCount;
    }

    public async Task<List<User>> GetMergeCandidatesAsync()
    {
        return await _context.Users.IgnoreQueryFilters()
            .Include(u => u.Group)
                .ThenInclude(g => g.Members)
            .ToListAsync();
    }

    public async Task<bool> IsAdministratorAsync(Guid userId)
    {
        User? user = await _context.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == userId);
        return user?.IsAdministrator ?? false;
    }

    public async Task<MergeOperationResult> MergeAccountsAsync(Guid user1Id, Guid user2Id, Guid adminId)
    {
        if (user1Id == user2Id)
        {
            return MergeOperationResult.Failure("Cannot merge same user.");
        }

        User? user1 = await _context.Users.IgnoreQueryFilters().Include(u => u.Group).ThenInclude(g => g.Members).FirstOrDefaultAsync(u => u.Id == user1Id);
        User? user2 = await _context.Users.IgnoreQueryFilters().Include(u => u.Group).ThenInclude(g => g.Members).FirstOrDefaultAsync(u => u.Id == user2Id);

        if (user1 == null || user2 == null)
        {
            return MergeOperationResult.Failure("User not found.");
        }

        UserGroup? group1 = user1.Group;
        UserGroup? group2 = user2.Group;

        if (group1 == null || group2 == null)
        {
            return MergeOperationResult.Failure("One or both users have no group.");
        }

        if (group1.Id == group2.Id)
        {
             return MergeOperationResult.Failure("Users are already in the same group.");
        }

        // Decide which group to keep
        // Prefer larger group
        UserGroup g1 = group1!;
        UserGroup g2 = group2!;

        int count1 = g1.Members?.Count ?? 0;
        int count2 = g2.Members?.Count ?? 0;

        UserGroup keptGroup = count1 >= count2 ? g1 : g2;
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
                        System.Reflection.MethodInfo? method = GetType()
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
        if (discardedGroup.Members != null)
        {
            foreach (User? member in discardedGroup.Members.ToList())
            {
                member.GroupId = keptGroupId;
                member.Group = keptGroup; // Ensure navigation property is updated if tracked
            }
        }

        // Delete discarded group
        _context.UserGroups.Remove(discardedGroup);

        await _context.SaveChangesAsync();

        LogMerge(_logger, discardedGroupId, keptGroupId, adminId, DateTime.UtcNow, null);

        return MergeOperationResult.Ok(keptGroup.Name, keptGroup.Members != null ? keptGroup.Members.Count : 0);
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
