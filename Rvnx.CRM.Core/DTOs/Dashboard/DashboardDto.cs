namespace Rvnx.CRM.Core.DTOs.Dashboard;

public class DashboardDto
{
    public List<UpcomingEventDto> UpcomingEvents { get; set; } = new();
    public List<GraphNodeDto> GraphNodes { get; set; } = new();
    public List<GraphLinkDto> GraphLinks { get; set; } = new();
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
}

public class GraphLinkDto
{
    public string Source { get; set; } = string.Empty;
    public string Target { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
}
