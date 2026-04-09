using Rvnx.CRM.Core.DTOs.Contact;
using Rvnx.CRM.Core.Models;
using Rvnx.CRM.Core.Models.Activity;

namespace Rvnx.CRM.Core.Interfaces;

public interface IActivityService
{
    Task<OperationResult> CreateAsync(ActivityFormDto dto);
    Task<OperationResult> UpdateAsync(Guid id, ActivityFormDto dto);
    Task<OperationResult> DeleteAsync(Guid id);
    Task<ActivityFormDto?> GetFormAsync(Guid id);
    Task<ActivityFormDto?> GetFormForCreateAsync(Guid entityId);
    Task<Activity?> GetByIdAsync(Guid id);
}