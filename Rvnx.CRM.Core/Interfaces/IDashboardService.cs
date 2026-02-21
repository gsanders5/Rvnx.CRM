using Rvnx.CRM.Core.DTOs.Dashboard;

namespace Rvnx.CRM.Core.Interfaces;

public interface IDashboardService
{
    Task<DashboardDto> GetDashboardDataAsync();
}
