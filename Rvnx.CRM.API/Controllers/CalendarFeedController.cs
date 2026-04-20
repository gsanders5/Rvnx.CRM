using System.ComponentModel.DataAnnotations;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rvnx.CRM.API.Authentication;
using Rvnx.CRM.Core.DTOs.Calendar;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models;

namespace Rvnx.CRM.API.Controllers;

/// <summary>
/// Subscribable iCalendar (RFC 5545) feed of a user's significant dates and incomplete tasks.
/// Intended for consumption by calendar clients (Google Calendar, Apple Calendar, Outlook) via URL subscription.
/// </summary>
/// <remarks>
/// Unlike every other endpoint in this API, this endpoint does NOT use Bearer header authentication.
/// Calendar clients cannot attach custom Authorization headers to subscription URLs, so authentication
/// is performed via a required <c>token</c> query-string parameter instead. The token value is the
/// same API token used elsewhere (prefix <c>crm_</c>); generate one from the user settings page and
/// append it as <c>?token=crm_...</c> when subscribing.
/// </remarks>
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

    /// <summary>
    /// Return an iCalendar (.ics) feed containing the authenticated user's significant dates and incomplete tasks.
    /// </summary>
    /// <remarks>
    /// Response is an RFC 5545 VCALENDAR with one VEVENT per significant date and per incomplete task.
    /// Event UIDs are deterministic (<c>{type}-{contactId}-{yyyyMMdd}-{titleHash}@rvnx-crm</c>), so subscribed
    /// calendar clients de-duplicate events correctly across refreshes.
    ///
    /// Subscribe from a calendar client by pasting the full URL (including <c>?token=</c>) as a new
    /// calendar subscription. Example: <c>https://crm.example.com/api/calendar/feed.ics?token=crm_xxxxxxxx</c>.
    /// </remarks>
    /// <param name="token">
    /// Required. The API token used to authenticate the request. Same value as the Bearer token used
    /// elsewhere in this API — the <c>crm_</c>-prefixed string generated from the user settings page.
    /// </param>
    /// <response code="200">iCalendar feed (<c>text/calendar; charset=utf-8</c>) as a downloadable file.</response>
    /// <response code="401">Token is missing, unknown, or revoked.</response>
    [HttpGet("feed.ics")]
    [Produces("text/calendar")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
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
