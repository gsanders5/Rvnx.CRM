using Rvnx.CRM.Core.DTOs.Contact;
using Rvnx.CRM.Core.Models;
using Rvnx.CRM.Core.Models.Contact;

namespace Rvnx.CRM.Core.Interfaces;

public interface IPetService
{
    Task<OperationResult> CreateAsync(PetFormDto dto);
    Task<OperationResult> UpdateAsync(Guid id, PetFormDto dto);
    Task<OperationResult> DeleteAsync(Guid id);
    Task<PetFormDto?> GetFormAsync(Guid id);
    Task<PetFormDto?> GetFormForCreateAsync(Guid entityId);
    Task<Pet?> GetByIdAsync(Guid id);
}
