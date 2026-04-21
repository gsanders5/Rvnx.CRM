namespace Rvnx.CRM.Core.DTOs.Dashboard;

public class DashboardDto
{
    public List<UpcomingEventDto> UpcomingEvents { get; set; } = [];
    public List<GraphNodeDto> GraphNodes { get; set; } = [];
    public List<GraphLinkDto> GraphLinks { get; set; } = [];
    public List<RecentContactDto> RecentContacts { get; set; } = [];
    public DashboardStatsDto Stats { get; set; } = new();
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
    public Guid? RelatedEntityId { get; set; }
    public string RelatedEntityName { get; set; } = string.Empty;
    public string TimeUntil { get; set; } = string.Empty;
}

public class GraphNodeDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Group { get; set; }
    public string? PhotoUrl { get; set; }
    public string? Gender { get; set; }
}

public class GraphLinkDto
{
    public string Source { get; set; } = string.Empty;
    public string Target { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
}
