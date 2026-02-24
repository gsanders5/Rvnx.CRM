#pragma warning disable CA1304, CA1311, CA1862
using Rvnx.CRM.Core.DTOs.Contact;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models.Contact;

namespace Rvnx.CRM.Core.Services;

public class LabelService(IRepository repository) : ILabelService
{
    private readonly IRepository _repository = repository;

    public async Task<List<LabelDto>> GetAllAsync()
    {
        List<Label> labels = await _repository.ListAsNoTrackingAsync<Label>(l => true) ?? [];
        return [.. labels.OrderBy(l => l.Name).Select(l => new LabelDto
        {
            Id = l.Id,
            Name = l.Name,
            Color = l.Color
        })];
    }

    public async Task<LabelOperationResult> CreateAsync(string name, string? color)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return LabelOperationResult.Failure("Label name cannot be empty.");
        }

        string testName = name.ToLower();
        List<Label> candidates = await _repository.ListAsNoTrackingAsync<Label>(l => l.Name.ToLower() == testName) ?? [];
        if (candidates.Any(e => e.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
        {
            return LabelOperationResult.Failure($"A label with the name '{name}' already exists.");
        }

        Label label = new()
        {
            Id = Guid.NewGuid(),
            Name = name,
            Color = color
        };

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

        string testName = name.ToLower();
        List<Label> candidates = await _repository.ListAsNoTrackingAsync<Label>(l => l.Id != id && l.Name.ToLower() == testName) ?? [];
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
        List<ContactLabel> existing = await _repository.ListAsync<ContactLabel>(cl => cl.ContactId == contactId && cl.LabelId == labelId) ?? [];
        if (existing.Count == 0)
        {
            ContactLabel contactLabel = new()
            {
                Id = Guid.NewGuid(),
                ContactId = contactId,
                LabelId = labelId
            };
            await _repository.AddAsync(contactLabel);
            await _repository.SaveChangesAsync();
        }
    }

    public async Task RemoveLabelAsync(Guid contactId, Guid labelId)
    {
        List<ContactLabel> existing = await _repository.ListAsync<ContactLabel>(cl => cl.ContactId == contactId && cl.LabelId == labelId) ?? [];
        ContactLabel? toRemove = existing.FirstOrDefault();
        if (toRemove != null)
        {
            await _repository.DeleteAsync<ContactLabel>(toRemove.Id);
            await _repository.SaveChangesAsync();
        }
    }

    public async Task<List<LabelDto>> GetLabelsForContactAsync(Guid contactId)
    {
        List<ContactLabel> contactLabels = await _repository.ListAsNoTrackingAsync<ContactLabel>(cl => cl.ContactId == contactId, default, nameof(ContactLabel.Label)) ?? [];
        return [.. contactLabels.Select(cl => cl.Label).OrderBy(l => l.Name).Select(l => new LabelDto
        {
            Id = l.Id,
            Name = l.Name,
            Color = l.Color
        })];
    }
}
