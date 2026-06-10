namespace Rvnx.CRM.Core.DTOs.Dashboard;

public class DashboardDto
{
    public List<UpcomingEventDto> UpcomingEvents { get; set; } = [];
    public List<GraphNodeDto> GraphNodes { get; set; } = [];
    public List<GraphLinkDto> GraphLinks { get; set; } = [];
    public List<RecentContactDto> RecentContacts { get; set; } = [];
    public List<OpenTaskDto> OpenTasks { get; set; } = [];
    public DashboardStatsDto Stats { get; set; } = new();
}

public class OpenTaskDto
{
    public Guid TaskId { get; set; }
    public Guid ContactId { get; set; }
    public string ContactName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public DateOnly DueDate { get; set; }
    public int DaysOverdue { get; set; }
    public bool IsOverdue => DaysOverdue > 0;
}

public class RecentContactDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public DateTime LastChangedDate { get; set; }
    public DateTime CreatedDate { get; set; }
    public Guid? ProfileImageId { get; set; }

    /// <summary>True when the contact was created within the last 7 days.</summary>
    public bool IsNew { get; set; }
}

public class DashboardStatsDto
{
    public int TotalContacts { get; set; }
    public int ContactsWithBirthday { get; set; }
    public int ContactsWithRelationships { get; set; }
    public int ContactsHidden { get; set; }
}

public class UpcomingEventDto
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string Type { get; set; } = string.Empty;
    public Guid? RelatedContactId { get; set; }
    public string RelatedContactName { get; set; } = string.Empty;
    public string TimeUntil { get; set; } = string.Empty;
}

public class GraphNodeDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Group { get; set; }
    public string? PhotoUrl { get; set; }
    public string? Gender { get; set; }

    /// <summary>True when the contact is marked deceased; rendered as a flower glyph in the network graph.</summary>
    public bool IsDeceased { get; set; }
}

public class GraphLinkDto
{
    public string Source { get; set; } = string.Empty;
    public string Target { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
}
