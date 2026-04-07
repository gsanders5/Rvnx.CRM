using Rvnx.CRM.Core.DTOs.Contact;
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

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA1862:Prefer using 'string.Equals(string, StringComparison)' to perform a case-insensitive comparison", Justification = "EF Core SQLite translation requires parameterless .ToLower() for case-insensitive comparisons.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1304:Specify CultureInfo", Justification = "EF Core SQLite translation requires parameterless .ToLower() for case-insensitive comparisons.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1311:Specify a culture or use an invariant version", Justification = "EF Core SQLite translation requires parameterless .ToLower() for case-insensitive comparisons.")]
    public async Task<LabelOperationResult> CreateAsync(string name, string? color)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return LabelOperationResult.Failure("Label name cannot be empty.");
        }

        string testName = name.ToLower();
        List<Label> candidates =
            await _repository.ListAsNoTrackingAsync<Label>(l => l.Name.ToLower() == testName) ?? [];
        if (candidates.Any(e => e.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
        {
            return LabelOperationResult.Failure($"A label with the name '{name}' already exists.");
        }

        Label label = new() { Id = Guid.NewGuid(), Name = name, Color = color };

        await _repository.AddAsync(label);
        await _repository.SaveChangesAsync();

        return LabelOperationResult.Ok(label.Id);
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA1862:Prefer using 'string.Equals(string, StringComparison)' to perform a case-insensitive comparison", Justification = "EF Core SQLite translation requires parameterless .ToLower() for case-insensitive comparisons.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1304:Specify CultureInfo", Justification = "EF Core SQLite translation requires parameterless .ToLower() for case-insensitive comparisons.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1311:Specify a culture or use an invariant version", Justification = "EF Core SQLite translation requires parameterless .ToLower() for case-insensitive comparisons.")]
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

        string testName = name.ToLower();
        List<Label> candidates =
            await _repository.ListAsNoTrackingAsync<Label>(l => l.Id != id && l.Name.ToLower() == testName) ?? [];
        if (candidates.Any(e => e.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
        {
            return LabelOperationResult.Failure($"A label with the name '{name}' already exists.");
        }

        label.Name = name;
        label.Color = color;

        await _repository.UpdateAsync(label);
        await _repository.SaveChangesAsync();

        return LabelOperationResult.Ok(label.Id);
    }

    public async Task DeleteAsync(Guid id)
    {
        Label? label = await _repository.GetByIdAsync<Label>(id);
        if (label != null)
        {
            await _repository.DeleteAsync<Label>(id);
            await _repository.SaveChangesAsync();
        }
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
        // ⚡ Bolt: Use bulk delete to avoid fetching the entity into memory and save a database roundtrip
        await _repository.DeleteAsync<ContactLabel>(cl => cl.ContactId == contactId && cl.LabelId == labelId);
        await _repository.SaveChangesAsync();
    }

    public async Task<List<LabelDto>> GetLabelsForContactAsync(Guid contactId)
    {
        // ⚡ Bolt Optimization: Use ListProjectedAsync to fetch only the required Label properties
        // instead of loading full ContactLabel and joined Label entities into memory.
        return await _repository.ListProjectedAsync<ContactLabel, LabelDto, string>(
            cl => cl.ContactId == contactId,
            cl => new LabelDto { Id = cl.Label.Id, Name = cl.Label.Name, Color = cl.Label.Color },
            orderBy: cl => cl.Label.Name,
            descending: false
        ) ?? [];
    }
}