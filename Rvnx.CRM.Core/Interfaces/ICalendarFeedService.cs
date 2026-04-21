using Rvnx.CRM.Core.DTOs.Calendar;

namespace Rvnx.CRM.Core.Interfaces;

public interface ICalendarFeedService
{
    string BuildIcsFeed(
        IEnumerable<CalendarEventDto> significantDateEvents,
        IEnumerable<CalendarEventDto> taskEvents);
}
