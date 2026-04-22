using Rvnx.CRM.Core.DTOs.Calendar;
using Rvnx.CRM.Infrastructure.Services;
using IcalCalendar = Ical.Net.Calendar;

namespace Rvnx.CRM.Tests.Services;

public class CalendarFeedServiceTests
{
    private readonly CalendarFeedService _service = new();

    [Fact]
    public void BuildIcsFeedProducesParsableCalendarWithExpectedEventCount()
    {
        Guid contactA = Guid.NewGuid();
        Guid contactB = Guid.NewGuid();

        List<CalendarEventDto> dateEvents =
        [
            new CalendarEventDto
            {
                Title = "Alice's Birthday",
                Start = "2026-05-01",
                Color = "#ff0000",
                AllDay = true,
                ContactId = contactA,
                Url = "https://example.com/contacts/alice",
            },
        ];

        List<CalendarEventDto> taskEvents =
        [
            new CalendarEventDto
            {
                Title = "Bob: Follow up",
                Start = "2026-06-15",
                Color = "#00ff00",
                AllDay = true,
                ContactId = contactB,
            },
        ];

        string ics = _service.BuildIcsFeed(dateEvents, taskEvents);

        IcalCalendar parsed = IcalCalendar.Load(ics);
        Assert.Equal(2, parsed.Events.Count);
        Assert.Contains(parsed.Events, e => e.Summary == "Alice's Birthday");
        Assert.Contains(parsed.Events, e => e.Summary == "Bob: Follow up");
    }

    [Fact]
    public void BuildIcsFeedProducesStableUidsAcrossInvocations()
    {
        Guid contactId = Guid.NewGuid();

        List<CalendarEventDto> dateEvents =
        [
            new CalendarEventDto
            {
                Title = "Alice's Birthday",
                Start = "2026-05-01",
                AllDay = true,
                ContactId = contactId,
            },
        ];

        List<CalendarEventDto> taskEvents =
        [
            new CalendarEventDto
            {
                Title = "Alice: Call",
                Start = "2026-07-04",
                AllDay = true,
                ContactId = contactId,
            },
        ];

        string firstIcs = _service.BuildIcsFeed(dateEvents, taskEvents);
        string secondIcs = _service.BuildIcsFeed(dateEvents, taskEvents);

        IcalCalendar firstParsed = IcalCalendar.Load(firstIcs);
        IcalCalendar secondParsed = IcalCalendar.Load(secondIcs);

        string[] firstUids = firstParsed.Events.Select(e => e.Uid).OrderBy(u => u).ToArray();
        string[] secondUids = secondParsed.Events.Select(e => e.Uid).OrderBy(u => u).ToArray();

        Assert.Equal(firstUids, secondUids);
        Assert.Contains(firstUids, u => u.StartsWith("date-", StringComparison.Ordinal) && u.EndsWith("@rvnx-crm", StringComparison.Ordinal));
        Assert.Contains(firstUids, u => u.StartsWith("task-", StringComparison.Ordinal) && u.EndsWith("@rvnx-crm", StringComparison.Ordinal));
    }

    [Fact]
    public void BuildIcsFeedProducesDistinctUidsForSameContactSameDayDifferentTitles()
    {
        Guid contactId = Guid.NewGuid();

        List<CalendarEventDto> taskEvents =
        [
            new CalendarEventDto { Title = "Call", Start = "2026-06-15", ContactId = contactId },
            new CalendarEventDto { Title = "Email", Start = "2026-06-15", ContactId = contactId },
        ];

        string ics = _service.BuildIcsFeed(Array.Empty<CalendarEventDto>(), taskEvents);
        IcalCalendar parsed = IcalCalendar.Load(ics);

        Assert.Equal(2, parsed.Events.Count);
        string[] uids = parsed.Events.Select(e => e.Uid).Distinct().ToArray();
        Assert.Equal(2, uids.Length);
    }

    [Fact]
    public void BuildIcsFeedSkipsEventsWithUnparsableStart()
    {
        List<CalendarEventDto> dateEvents =
        [
            new CalendarEventDto
            {
                Title = "Valid",
                Start = "2026-05-01",
                ContactId = Guid.NewGuid(),
            },
            new CalendarEventDto
            {
                Title = "Invalid",
                Start = "not-a-date",
                ContactId = Guid.NewGuid(),
            },
        ];

        string ics = _service.BuildIcsFeed(dateEvents, Array.Empty<CalendarEventDto>());
        IcalCalendar parsed = IcalCalendar.Load(ics);

        Assert.Single(parsed.Events);
        Assert.Equal("Valid", parsed.Events.First().Summary);
    }
}
