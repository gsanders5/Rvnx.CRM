using Rvnx.CRM.Core.DTOs.Calendar;
using Rvnx.CRM.Core.DTOs.Contact;
using Rvnx.CRM.Core.Models;
using Rvnx.CRM.Core.Models.Contact;

namespace Rvnx.CRM.Core.Interfaces;

public interface IContactTaskService
{
    Task<List<ContactTaskDto>> GetByContactAsync(Guid contactId);
    Task<OperationResult> CreateAsync(ContactTaskFormDto dto);
    Task<OperationResult> UpdateAsync(Guid id, ContactTaskFormDto dto);
    Task<OperationResult> DeleteAsync(Guid id);
    Task<ContactTaskFormDto?> GetFormAsync(Guid id);
    Task<ContactTaskFormDto?> GetFormForCreateAsync(Guid contactId);
    Task<ContactTask?> GetByIdAsync(Guid id);
    Task<OperationResult> ToggleCompleteAsync(Guid id);
    Task<List<CalendarEventDto>> GetCalendarEventsAsync();
}
