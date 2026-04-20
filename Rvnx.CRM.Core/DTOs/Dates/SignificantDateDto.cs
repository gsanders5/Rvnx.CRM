using Rvnx.CRM.Core.DTOs.Base;
using Rvnx.CRM.Core.Enumerations;

namespace Rvnx.CRM.Core.DTOs.Dates;

public class SignificantDateDto : BaseDto
{
    public string Title { get; set; } = string.Empty;
    public DateOnly EventDate { get; set; }
    public string? Description { get; set; }
    public Guid EntityId { get; set; }
    public EntityType EntityType { get; set; } = EntityType.Person;
    public Enumerations.RecurrenceType RecurrenceType { get; set; }
    public int? CustomIntervalDays { get; set; }
    public bool IsActive { get; set; } = true;
    public DateOnly? NextOccurrence { get; set; }

    public List<ReminderOffsetDto> ReminderOffsets { get; set; } = [];
}