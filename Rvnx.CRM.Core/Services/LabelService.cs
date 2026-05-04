using Rvnx.CRM.Core.DTOs.Base;
using Rvnx.CRM.Core.DTOs.Contact;
using Rvnx.CRM.Core.Extensions;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models.Contact;

namespace Rvnx.CRM.Core.Services;

public class LabelService(IRepository repository) : ILabelService
{
    private readonly IRepository _repository = repository;

    public async Task<List<LabelDto>> GetAllAsync()
    {
        return await _repository.ListProjectedAsync<Label, LabelDto, string>(
            l => true,
            l => new LabelDto { Id = l.Id, Name = l.Name, Color = l.Color },
            orderBy: l => l.Name,
            descending: false
        );
    }

    public async Task<LabelDto?> GetByIdAsync(Guid id)
    {
        Label? label = await _repository.GetByIdAsync<Label>(id);
        return label == null ? null : new LabelDto { Id = label.Id, Name = label.Name, Color = label.Color };
    }

    public async Task<LabelOperationResult> CreateAsync(string name, string? color)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return LabelOperationResult.Failure("Label name cannot be empty.");
        }

        if (await NameConflictExistsAsync(name, excludeId: null))
        {
            return LabelOperationResult.Failure($"A label with the name '{name}' already exists.");
        }

        Label label = new() { Id = Guid.NewGuid(), Name = name, Color = color };

        await _repository.AddAsync(label);
        await _repository.SaveChangesAsync();

        return LabelOperationResult.Ok(label.Id);
    }

    public async Task<LabelOperationResult> UpdateAsync(Guid id, string name, string? color)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return LabelOperationResult.Failure("Label name cannot be empty.");
        }

        Label? label = await _repository.GetByIdAsync<Label>(id);
        if (label == null)
        {
            return LabelOperationResult.NotFound();
        }

        if (await NameConflictExistsAsync(name, excludeId: id))
        {
            return LabelOperationResult.Failure($"A label with the name '{name}' already exists.");
        }

        label.Name = name;
        label.Color = color;

        await _repository.UpdateAsync(label);
        await _repository.SaveChangesAsync();

        return LabelOperationResult.Ok(label.Id);
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA1862:Prefer using 'string.Equals(string, StringComparison)' to perform a case-insensitive comparison", Justification = "EF Core SQLite translation requires parameterless .ToLower() for case-insensitive comparisons.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1304:Specify CultureInfo", Justification = "EF Core SQLite translation requires parameterless .ToLower() for case-insensitive comparisons.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1311:Specify a culture or use an invariant version", Justification = "EF Core SQLite translation requires parameterless .ToLower() for case-insensitive comparisons.")]
    private async Task<bool> NameConflictExistsAsync(string name, Guid? excludeId)
    {
        string testName = name.ToLower();
        List<Label> candidates = await _repository.ListAsNoTrackingAsync<Label>(
            l => (excludeId == null || l.Id != excludeId) && l.Name.ToLower() == testName) ?? [];
        return candidates.Any(e => e.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    public async Task DeleteAsync(Guid id)
    {
        await _repository.DeleteAsync<Label>(l => l.Id == id);
        await _repository.SaveChangesAsync();
    }

    public async Task AssignLabelAsync(Guid contactId, Guid labelId)
    {
        int existingCount = await _repository.CountAsync<ContactLabel>(cl => cl.ContactId == contactId && cl.LabelId == labelId);
        if (existingCount == 0)
        {
            ContactLabel contactLabel = new() { Id = Guid.NewGuid(), ContactId = contactId, LabelId = labelId };
            await _repository.AddAsync(contactLabel);
            await _repository.SaveChangesAsync();
        }
    }

    public async Task RemoveLabelAsync(Guid contactId, Guid labelId)
    {
        await _repository.DeleteAsync<ContactLabel>(cl => cl.ContactId == contactId && cl.LabelId == labelId);
        await _repository.SaveChangesAsync();
    }

    public async Task<BulkOperationResult> BulkAssignLabelAsync(IReadOnlyCollection<Guid> contactIds, Guid labelId)
    {
        if (contactIds.Count == 0)
        {
            return BulkOperationResult.Ok(0);
        }

        Label? label = await _repository.GetByIdAsync<Label>(labelId);
        if (label == null)
        {
            return BulkOperationResult.Fail("Label not found.");
        }

        HashSet<Guid> distinctIds = [.. contactIds];

        List<Guid> existingPairs = await _repository.ListProjectedByChunkedContainsAsync<ContactLabel, Guid, Guid>(
            [.. distinctIds],
            chunk => cl => cl.LabelId == labelId && chunk.Contains(cl.ContactId),
            cl => cl.ContactId);
        HashSet<Guid> alreadyAssigned = [.. existingPairs];

        List<Guid> existingContacts = await _repository.ListProjectedByChunkedContainsAsync<Contact, Guid, Guid>(
            [.. distinctIds],
            chunk => c => chunk.Contains(c.Id),
            c => c.Id);
        HashSet<Guid> validContactIds = [.. existingContacts];

        List<ContactLabel> toAdd = [];
        int skipped = 0;
        foreach (Guid id in distinctIds)
        {
            if (!validContactIds.Contains(id))
            {
                skipped++;
                continue;
            }
            if (alreadyAssigned.Contains(id))
            {
                skipped++;
                continue;
            }
            toAdd.Add(new ContactLabel { Id = Guid.NewGuid(), ContactId = id, LabelId = labelId });
        }

        if (toAdd.Count > 0)
        {
            await _repository.AddRangeAsync(toAdd);
            await _repository.SaveChangesAsync();
        }

        return BulkOperationResult.Ok(toAdd.Count, skipped);
    }

    public async Task<BulkOperationResult> BulkRemoveLabelAsync(IReadOnlyCollection<Guid> contactIds, Guid labelId)
    {
        if (contactIds.Count == 0)
        {
            return BulkOperationResult.Ok(0);
        }

        HashSet<Guid> distinctIds = [.. contactIds];

        List<Guid> matchedContactIds = await _repository.ListProjectedByChunkedContainsAsync<ContactLabel, Guid, Guid>(
            [.. distinctIds],
            chunk => cl => cl.LabelId == labelId && chunk.Contains(cl.ContactId),
            cl => cl.ContactId);

        if (matchedContactIds.Count > 0)
        {
            HashSet<Guid> matchedSet = [.. matchedContactIds];
            await _repository.DeleteAsync<ContactLabel>(cl => cl.LabelId == labelId && matchedSet.Contains(cl.ContactId));
            await _repository.SaveChangesAsync();
        }

        int skipped = distinctIds.Count - matchedContactIds.Count;
        return BulkOperationResult.Ok(matchedContactIds.Count, skipped);
    }

    public async Task<List<LabelDto>> GetLabelsForContactAsync(Guid contactId)
    {
        return await _repository.ListProjectedAsync<ContactLabel, LabelDto, string>(
            cl => cl.ContactId == contactId,
            cl => new LabelDto { Id = cl.Label.Id, Name = cl.Label.Name, Color = cl.Label.Color },
            orderBy: cl => cl.Label.Name,
            descending: false
        ) ?? [];
    }
}
