using Rvnx.CRM.Core.DTOs.Contact;
using Rvnx.CRM.Core.Models;
using Rvnx.CRM.Core.Models.Contact;

namespace Rvnx.CRM.Core.Interfaces;

public interface IContactMethodService
{
    Task<OperationResult> CreateAsync(ContactMethodFormDto dto);
    Task<OperationResult> UpdateAsync(Guid id, ContactMethodFormDto dto);
    Task<OperationResult> DeleteAsync(Guid id);
    Task<ContactMethodFormDto?> GetFormAsync(Guid id);
    Task<ContactMethodFormDto?> GetFormForCreateAsync(Guid entityId, string entityType);
    Task<ContactMethod?> GetByIdAsync(Guid id);
}
