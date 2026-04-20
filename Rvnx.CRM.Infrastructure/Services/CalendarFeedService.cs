using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;
using Ical.Net.Serialization;
using Rvnx.CRM.Core.DTOs.Calendar;
using Rvnx.CRM.Core.Interfaces;
using System.Globalization;
using IcalCalendar = Ical.Net.Calendar;

namespace Rvnx.CRM.Infrastructure.Services;

public class CalendarFeedService : ICalendarFeedService
{
    private const string DateEventType = "date";
    private const string TaskEventType = "task";

    public string BuildIcsFeed(
        IEnumerable<CalendarEventDto> significantDateEvents,
        IEnumerable<CalendarEventDto> taskEvents)
    {
        ArgumentNullException.ThrowIfNull(significantDateEvents);
        ArgumentNullException.ThrowIfNull(taskEvents);

        IcalCalendar calendar = new();
        calendar.AddProperty("PRODID", "-//Rvnx CRM//EN");

        DateTime stamp = DateTime.UtcNow;

        foreach (CalendarEventDto dto in significantDateEvents)
        {
            AddEvent(calendar, dto, DateEventType, stamp);
        }

        foreach (CalendarEventDto dto in taskEvents)
        {
            AddEvent(calendar, dto, TaskEventType, stamp);
        }

        return new CalendarSerializer().SerializeToString(calendar);
    }

    private static void AddEvent(IcalCalendar calendar, CalendarEventDto dto, string eventType, DateTime stamp)
    {
        if (!DateOnly.TryParseExact(dto.Start, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly startDate))
        {
            return;
        }

        string uid = $"{eventType}-{dto.ContactId:N}-{startDate:yyyyMMdd}@rvnx-crm";

        CalendarEvent calendarEvent = new()
        {
            Uid = uid,
            Summary = dto.Title,
            Start = new CalDateTime(startDate.Year, startDate.Month, startDate.Day),
            IsAllDay = true,
            DtStamp = new CalDateTime(stamp, "UTC"),
        };

        if (!string.IsNullOrWhiteSpace(dto.Url) && Uri.TryCreate(dto.Url, UriKind.Absolute, out Uri? parsedUrl))
        {
            calendarEvent.Url = parsedUrl;
        }

        calendar.Events.Add(calendarEvent);
    }
}
