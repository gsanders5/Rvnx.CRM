using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rvnx.CRM.Core.DTOs.Calendar;
using Rvnx.CRM.Core.Interfaces;

namespace Rvnx.CRM.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CalendarController(ISignificantDateService significantDateService, IContactTaskService contactTaskService) : ControllerBase
{
    private readonly ISignificantDateService _significantDateService = significantDateService;
    private readonly IContactTaskService _contactTaskService = contactTaskService;

    [HttpGet("events")]
    public async Task<IActionResult> Events()
    {
        Task<List<CalendarEventDto>> dateEventsTask = _significantDateService.GetCalendarEventsAsync();
        Task<List<CalendarEventDto>> taskEventsTask = _contactTaskService.GetCalendarEventsAsync();

        await Task.WhenAll(dateEventsTask, taskEventsTask);

        List<CalendarEventDto> allEvents = [.. dateEventsTask.Result, .. taskEventsTask.Result];
        return Ok(allEvents);
    }
}
