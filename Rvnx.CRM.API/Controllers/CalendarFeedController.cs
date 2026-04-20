using System.ComponentModel.DataAnnotations;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rvnx.CRM.API.Authentication;
using Rvnx.CRM.Core.DTOs.Calendar;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models;

namespace Rvnx.CRM.API.Controllers;

[ApiController]
[Route("api/calendar")]
[AllowAnonymous]
public class CalendarFeedController(
    ISignificantDateService significantDateService,
    IContactTaskService contactTaskService,
    ICalendarFeedService calendarFeedService,
    IApiTokenService apiTokenService) : ControllerBase
{
    private readonly ISignificantDateService _significantDateService = significantDateService;
    private readonly IContactTaskService _contactTaskService = contactTaskService;
    private readonly ICalendarFeedService _calendarFeedService = calendarFeedService;
    private readonly IApiTokenService _apiTokenService = apiTokenService;

    [HttpGet("feed.ics")]
    public async Task<IActionResult> Feed([FromQuery, Required] string token)
    {
        ApiToken? resolvedToken = await _apiTokenService.ResolveTokenAsync(token);
        if (resolvedToken == null)
        {
            return Unauthorized();
        }

        HttpContext.Items[ApiTokenAuthenticationOptions.ResolvedTokenItemKey] = resolvedToken;

        Task<List<CalendarEventDto>> dateEventsTask = _significantDateService.GetCalendarEventsAsync();
        Task<List<CalendarEventDto>> taskEventsTask = _contactTaskService.GetCalendarEventsAsync();

        await Task.WhenAll(dateEventsTask, taskEventsTask);

        string ics = _calendarFeedService.BuildIcsFeed(dateEventsTask.Result, taskEventsTask.Result);

        return File(Encoding.UTF8.GetBytes(ics), "text/calendar; charset=utf-8", "rvnx-calendar.ics");
    }
}
