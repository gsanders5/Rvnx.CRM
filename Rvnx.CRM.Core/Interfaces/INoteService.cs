using Rvnx.CRM.Core.DTOs.Base;
using Rvnx.CRM.Core.Models;
using Rvnx.CRM.Core.Models.Base;

namespace Rvnx.CRM.Core.Interfaces;

public interface INoteService
{
    Task<OperationResult> CreateAsync(NoteFormViewModel dto);
    Task<OperationResult> UpdateAsync(Guid id, NoteFormViewModel dto);
    Task<OperationResult> DeleteAsync(Guid id);
    Task<NoteFormViewModel?> GetFormAsync(Guid id);
    Task<NoteFormViewModel?> GetFormForCreateAsync(Guid entityId, string entityType);
    Task<Note?> GetByIdAsync(Guid id);
}
