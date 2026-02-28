using Rvnx.CRM.Core.Enumerations;

namespace Rvnx.CRM.Core.DTOs.Dates;

public class UpdateSignificantDateRequest
{
    public Guid Id { get; set; }
    public Guid EntityId { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateOnly EventDate { get; set; }
    public RecurrenceType RecurrenceType { get; set; }
    public int? CustomIntervalDays { get; set; }
    public bool IsActive { get; set; }
}
