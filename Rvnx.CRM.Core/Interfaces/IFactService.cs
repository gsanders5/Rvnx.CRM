using Rvnx.CRM.Core.DTOs.Contact;
using Rvnx.CRM.Core.Models;
using Rvnx.CRM.Core.Models.Contact;

namespace Rvnx.CRM.Core.Interfaces;

public interface IFactService
{
    Task<OperationResult> CreateAsync(FactFormDto dto);
    Task<OperationResult> UpdateAsync(Guid id, FactFormDto dto);
    Task<OperationResult> DeleteAsync(Guid id);
    Task<FactFormDto?> GetFormAsync(Guid id);
    Task<FactFormDto?> GetFormForCreateAsync(Guid entityId, string entityType);
    Task<Fact?> GetByIdAsync(Guid id);
}
