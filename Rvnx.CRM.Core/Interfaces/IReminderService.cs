using Rvnx.CRM.Core.DTOs.Dates;
using Rvnx.CRM.Core.Models;
using Rvnx.CRM.Core.Models.Dates;

namespace Rvnx.CRM.Core.Interfaces;

public interface IReminderService
{
    Task<OperationResult> CreateAsync(ReminderFormViewModel dto);
    Task<OperationResult> UpdateAsync(Guid id, ReminderFormViewModel dto);
    Task<OperationResult> DeleteAsync(Guid id);
    Task<ReminderFormViewModel?> GetFormAsync(Guid id);
    Task<ReminderFormViewModel?> GetFormForCreateAsync(Guid entityId, string entityType);
    Task<ReminderDto?> GetDtoAsync(Guid id);
}
