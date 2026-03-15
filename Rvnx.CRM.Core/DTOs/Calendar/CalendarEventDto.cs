namespace Rvnx.CRM.Core.DTOs.Calendar;

public class CalendarEventDto
{
    public string Title { get; set; } = string.Empty;
    public string Start { get; set; } = string.Empty;
    public string? Url { get; set; }
    public string Color { get; set; } = string.Empty;
    public bool AllDay { get; set; } = true;
    public Guid ContactId { get; set; }
}