using Microsoft.AspNetCore.Mvc;
using Rvnx.CRM.Core.DTOs.Calendar;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Web.Controllers.Base;

namespace Rvnx.CRM.Web.Controllers;

public class CalendarController(ISignificantDateService significantDateService) : AuthorizedController
{
    private readonly ISignificantDateService _significantDateService = significantDateService;

    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> Events()
    {
        List<CalendarEventDto> events = await _significantDateService.GetCalendarEventsAsync();
        foreach (CalendarEventDto evt in events)
        {
            evt.Url = Url.Action("Details", "Contacts", new { id = evt.ContactId });
        }
        return Json(events);
    }
}