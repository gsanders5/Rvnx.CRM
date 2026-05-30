using Microsoft.AspNetCore.Mvc;
using Rvnx.CRM.Core.DTOs.Calendar;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Web.Controllers.Base;
using Rvnx.CRM.Web.ViewModels.Calendar;
using System.Globalization;


namespace Rvnx.CRM.Web.Controllers;

public class CalendarController(ISignificantDateService significantDateService, IContactTaskService contactTaskService) : AuthorizedController
{
    private readonly ISignificantDateService _significantDateService = significantDateService;
    private readonly IContactTaskService _contactTaskService = contactTaskService;

    [HttpGet]
    public IActionResult Index(DateOnly? date)
    {
        return View(new CalendarIndexViewModel
        {
            InitialDate = (date ?? DateOnly.FromDateTime(DateTime.Today)).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
        });
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
