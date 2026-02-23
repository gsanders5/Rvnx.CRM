using Rvnx.CRM.Core.DTOs.Contact;

namespace Rvnx.CRM.Core.Interfaces;

public interface ILabelService
{
    Task<List<LabelDto>> GetAllAsync();
    Task<LabelOperationResult> CreateAsync(string name, string? color);
    Task<LabelOperationResult> UpdateAsync(Guid id, string name, string? color);
    Task DeleteAsync(Guid id);
    Task AssignLabelAsync(Guid contactId, Guid labelId);
    Task RemoveLabelAsync(Guid contactId, Guid labelId);
    Task<List<LabelDto>> GetLabelsForContactAsync(Guid contactId);
}
