using Rvnx.CRM.Core.DTOs.Dashboard;

namespace Rvnx.CRM.Web.Models
{
    public class DashboardViewModel
    {
        public List<UpcomingEventDto> UpcomingEvents { get; set; } = new();
        public List<GraphNodeDto> GraphNodes { get; set; } = new();
        public List<GraphLinkDto> GraphLinks { get; set; } = new();
    }
}
