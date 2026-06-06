using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rvnx.CRM.Core.DTOs.Calendar;
using Rvnx.CRM.Core.Interfaces;

namespace Rvnx.CRM.API.Controllers;

/// <summary>
/// Read-only calendar view that aggregates significant dates and tasks into a unified event list.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CalendarController(ISignificantDateService significantDateService, IContactTaskService contactTaskService) : ControllerBase
{
    private readonly ISignificantDateService _significantDateService = significantDateService;
    private readonly IContactTaskService _contactTaskService = contactTaskService;

    /// <summary>
    /// Get all calendar events (significant dates and tasks combined) across all contacts.
    /// </summary>
    [HttpGet("events")]
    public async Task<IActionResult> Events()
    {
        Task<List<CalendarEventDto>> dateEventsTask = _significantDateService.GetCalendarEventsAsync();
        Task<List<CalendarEventDto>> taskEventsTask = _contactTaskService.GetCalendarEventsAsync();

        await Task.WhenAll(dateEventsTask, taskEventsTask);

        List<CalendarEventDto> allEvents = [.. await dateEventsTask, .. await taskEventsTask];
        return Ok(allEvents);
    }
}
