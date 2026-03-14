using Rvnx.CRM.Core.DTOs.Base;

namespace Rvnx.CRM.Core.DTOs.Dates;

public class ReminderOffsetDto : BaseDto
{
    public int DaysBeforeEvent { get; set; }
    public bool IsActive { get; set; }
    public DateOnly? ScheduledFor { get; set; }
}