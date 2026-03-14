using Rvnx.CRM.Core.DTOs.Dashboard;

namespace Rvnx.CRM.Core.Interfaces;

public interface IDashboardService
{
    /// <summary>
    /// Aggregates data for the user dashboard.
    /// Includes upcoming birthdays (next 30 days) and active reminders.
    /// Filters reminders at the database level for performance.
    /// </summary>
    /// <returns>A <see cref="DashboardDto"/> containing the aggregated data.</returns>
    Task<DashboardDto> GetDashboardDataAsync();
}