using Microsoft.AspNetCore.Mvc;
using Rvnx.CRM.Core.DTOs.Calendar;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Web.Controllers.Base;

namespace Rvnx.CRM.Web.Controllers;

public class CalendarController(ISignificantDateService significantDateService, IContactTaskService contactTaskService) : AuthorizedController
{
    private readonly ISignificantDateService _significantDateService = significantDateService;
    private readonly IContactTaskService _contactTaskService = contactTaskService;

    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> Events()
    {
        Task<List<CalendarEventDto>> dateTask = _significantDateService.GetCalendarEventsAsync();
        Task<List<CalendarEventDto>> taskTask = _contactTaskService.GetCalendarEventsAsync();
        await Task.WhenAll(dateTask, taskTask);
        List<CalendarEventDto> events = [.. dateTask.Result, .. taskTask.Result];
        foreach (CalendarEventDto evt in events)
        {
            evt.Url = Url.Action("Details", "Contacts", new { id = evt.ContactId });
        }
        return Json(events);
    }
}
