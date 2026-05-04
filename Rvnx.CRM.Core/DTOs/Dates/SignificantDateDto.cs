using Rvnx.CRM.Core.DTOs.Base;
using Rvnx.CRM.Core.Enumerations;

namespace Rvnx.CRM.Core.DTOs.Dates;

public record class SignificantDateDto : BaseDto
{
    public string Title { get; set; } = string.Empty;
    public DateOnly EventDate { get; set; }
    public string? Description { get; set; }
    public Guid ContactId { get; set; }
    public RecurrenceType RecurrenceType { get; set; }
    public int? CustomIntervalDays { get; set; }
    public bool IsActive { get; set; } = true;
    public DateOnly? NextOccurrence { get; set; }

    public List<ReminderOffsetDto> ReminderOffsets { get; set; } = [];
}
