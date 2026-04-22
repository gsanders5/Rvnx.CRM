using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;
using Ical.Net.Serialization;
using Rvnx.CRM.Core.DTOs.Calendar;
using Rvnx.CRM.Core.Interfaces;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
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

        return new CalendarSerializer().SerializeToString(calendar) ?? string.Empty;
    }

    private static void AddEvent(IcalCalendar calendar, CalendarEventDto dto, string eventType, DateTime stamp)
    {
        if (!DateOnly.TryParseExact(dto.Start, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly startDate))
        {
            return;
        }

        // Title hash disambiguates same-contact, same-day events (e.g. two tasks, or anniversary + "met on")
        // so calendar clients don't dedupe distinct events to a single entry.
        string titleHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(dto.Title ?? string.Empty)))[..8];
        string uid = $"{eventType}-{dto.ContactId:N}-{startDate:yyyyMMdd}-{titleHash}@rvnx-crm";

        // Ical.Net 5.x: a CalDateTime built from date-only components (no time) makes the
        // event all-day; IsAllDay is now derived and no longer directly assignable.
        CalendarEvent calendarEvent = new()
        {
            Uid = uid,
            Summary = dto.Title,
            Start = new CalDateTime(startDate.Year, startDate.Month, startDate.Day),
            DtStamp = new CalDateTime(stamp, "UTC"),
        };

        if (!string.IsNullOrWhiteSpace(dto.Url) && Uri.TryCreate(dto.Url, UriKind.Absolute, out Uri? parsedUrl))
        {
            calendarEvent.Url = parsedUrl;
        }

        calendar.Events.Add(calendarEvent);
    }
}
