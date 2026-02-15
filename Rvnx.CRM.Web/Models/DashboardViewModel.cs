namespace Rvnx.CRM.Web.Models
{
    public class DashboardViewModel
    {
        public List<UpcomingEventViewModel> UpcomingEvents { get; set; } = new();
        public List<GraphNode> GraphNodes { get; set; } = new();
        public List<GraphLink> GraphLinks { get; set; } = new();
    }

    public class UpcomingEventViewModel
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public string Type { get; set; } = string.Empty; // "Reminder", "Birthday", "Event"
        public Guid? RelatedEntityId { get; set; }
        public string RelatedEntityName { get; set; } = string.Empty;
        public string TimeUntil { get; set; } = string.Empty;
    }

    public class GraphNode
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int Group { get; set; } // For color grouping
    }

    public class GraphLink
    {
        public string Source { get; set; } = string.Empty;
        public string Target { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
    }
}
