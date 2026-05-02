using Rvnx.CRM.Core.Constants;
using Rvnx.CRM.Core.DTOs.Calendar;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Core.Models.Dates;
using Rvnx.CRM.Infrastructure.Data;
using Rvnx.CRM.Infrastructure.Repositories;
using Rvnx.CRM.Infrastructure.Services;
using Rvnx.CRM.Tests.Helpers;

namespace Rvnx.CRM.Tests.Services;

public class SignificantDateServiceTests : IDisposable
{
    private readonly CRMDbContext _context;
    private readonly Repository _repository;
    private readonly SignificantDateService _service;

    public SignificantDateServiceTests()
    {
        _context = TestDbContextFactory.Create(Guid.NewGuid(), "test-user", null, out _);
        _repository = new Repository(_context);
        _service = new SignificantDateService(_repository);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task GetCalendarEventsAsyncWithActiveDatesReturnsCorrectEvents()
    {
        // Arrange
        Guid contactId1 = Guid.NewGuid();
        Guid contactId2 = Guid.NewGuid();

        _context.Contacts!.AddRange(
            new Contact { Id = contactId1, FirstName = "John", LastName = "Doe" },
            new Contact { Id = contactId2, FirstName = "Jane", LastName = "Smith" }
        );

        DateOnly today = DateOnly.FromDateTime(DateTime.Today);

        _context.SignificantDates!.AddRange(
            new SignificantDate
            {
                Id = Guid.NewGuid(),
                ContactId = contactId1,
                Title = "Birthday",
                EventDate = today.AddDays(5),
                RecurrenceType = Core.Enumerations.RecurrenceType.Annual,
                IsActive = true
            },
            new SignificantDate
            {
                Id = Guid.NewGuid(),
                ContactId = contactId2,
                Title = "Anniversary",
                EventDate = today.AddDays(10),
                RecurrenceType = Core.Enumerations.RecurrenceType.Annual,
                IsActive = true
            },
            new SignificantDate
            {
                Id = Guid.NewGuid(),
                ContactId = contactId1,
                Title = "Inactive Event",
                EventDate = today.AddDays(15),
                RecurrenceType = Core.Enumerations.RecurrenceType.Annual,
                IsActive = false // Should not be included
            }
        );

        await _context.SaveChangesAsync();

        // Act
        List<CalendarEventDto> events = await _service.GetCalendarEventsAsync();

        // Assert
        Assert.NotNull(events);
        Assert.Equal(2, events.Count);

        CalendarEventDto? birthdayEvent = events.FirstOrDefault(e => e.Title.Contains("Birthday"));
        Assert.NotNull(birthdayEvent);
        Assert.Equal("John's Birthday", birthdayEvent.Title);
        Assert.Equal(CalendarColors.Birthday, birthdayEvent.Color);
        Assert.Equal(contactId1, birthdayEvent.ContactId);

        CalendarEventDto? anniversaryEvent = events.FirstOrDefault(e => e.Title.Contains("Anniversary"));
        Assert.NotNull(anniversaryEvent);
        Assert.Equal("Jane's Anniversary", anniversaryEvent.Title);
        Assert.Equal(CalendarColors.SignificantDate, anniversaryEvent.Color);
        Assert.Equal(contactId2, anniversaryEvent.ContactId);
    }

    [Fact]
    public async Task GetCalendarEventsAsyncWhenNoActiveDatesReturnsEmptyList()
    {
        Guid contactId = Guid.NewGuid();
        _context.Contacts!.Add(new Contact { Id = contactId, FirstName = "John", LastName = "Doe" });

        _context.SignificantDates!.Add(new SignificantDate
        {
            Id = Guid.NewGuid(),
            ContactId = contactId,
            Title = "Past Event",
            EventDate = new DateOnly(2000, 1, 1),
            RecurrenceType = Core.Enumerations.RecurrenceType.None,
            IsActive = false
        });

        await _context.SaveChangesAsync();

        List<CalendarEventDto> events = await _service.GetCalendarEventsAsync();

        Assert.Empty(events);
    }

    [Fact]
    public async Task GetCalendarEventsAsyncIncludesCurrentYearOccurrenceWhenApplicable()
    {
        Guid contactId = Guid.NewGuid();
        _context.Contacts!.Add(new Contact { Id = contactId, FirstName = "Alice", LastName = "Wonderland" });

        DateOnly today = DateOnly.FromDateTime(DateTime.Today);
        DateOnly eventDate = today.AddDays(-10); // Event was 10 days ago

        _context.SignificantDates!.Add(new SignificantDate
        {
            Id = Guid.NewGuid(),
            ContactId = contactId,
            Title = "Annual Meeting",
            EventDate = eventDate,
            RecurrenceType = Core.Enumerations.RecurrenceType.Annual,
            IsActive = true
        });

        await _context.SaveChangesAsync();

        List<CalendarEventDto> events = await _service.GetCalendarEventsAsync();

        // We expect two events: the next occurrence (next year) and the current year's occurrence
        Assert.Equal(2, events.Count);
        Assert.All(events, e => Assert.Equal("Alice's Annual Meeting", e.Title));
        Assert.Contains(events, e => e.Start == eventDate.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture));
        Assert.Contains(events, e => e.Start == eventDate.AddYears(1).ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture));
    }

    [Fact]
    public async Task GetCalendarEventsAsyncWithLeapYearBirthdayReturnsCorrectEventInNonLeapYear()
    {
        Guid contactId = Guid.NewGuid();
        _context.Contacts!.Add(new Contact { Id = contactId, FirstName = "Leap", LastName = "Day" });

        _context.SignificantDates!.Add(new SignificantDate
        {
            Id = Guid.NewGuid(),
            ContactId = contactId,
            Title = SignificantDateTitles.Birthday,
            EventDate = new DateOnly(2020, 2, 29),
            RecurrenceType = Core.Enumerations.RecurrenceType.Annual,
            IsActive = true
        });

        await _context.SaveChangesAsync();

        List<CalendarEventDto> events = await _service.GetCalendarEventsAsync();

        Assert.NotEmpty(events);

        // In a non-leap year Feb 29 clamps to Feb 28; in a leap year it stays Feb 29
        int currentYear = DateTime.Today.Year;
        int expectedDay = DateTime.IsLeapYear(currentYear) ? 29 : 28;
        Assert.All(events, e =>
        {
            DateOnly eventDate = DateOnly.ParseExact(e.Start, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
            Assert.Equal(2, eventDate.Month);
            Assert.Equal(expectedDay, eventDate.Day);
        });
    }

    [Fact]
    public async Task GetCalendarEventsAsyncWithInactiveReminderOffsetsStillGeneratesEvent()
    {
        Guid contactId = Guid.NewGuid();
        _context.Contacts!.Add(new Contact { Id = contactId, FirstName = "Reminder", LastName = "Test" });

        Guid significantDateId = Guid.NewGuid();
        DateOnly today = DateOnly.FromDateTime(DateTime.Today);

        _context.SignificantDates!.Add(new SignificantDate
        {
            Id = significantDateId,
            ContactId = contactId,
            Title = "Work Anniversary",
            EventDate = today.AddDays(30),
            RecurrenceType = Core.Enumerations.RecurrenceType.Annual,
            IsActive = true,
            ReminderOffsets =
            [
                new ReminderOffset { Id = Guid.NewGuid(), SignificantDateId = significantDateId, DaysBeforeEvent = 7, IsActive = false },
                new ReminderOffset { Id = Guid.NewGuid(), SignificantDateId = significantDateId, DaysBeforeEvent = 1, IsActive = false }
            ]
        });

        await _context.SaveChangesAsync();

        List<CalendarEventDto> events = await _service.GetCalendarEventsAsync();

        Assert.Single(events);
        Assert.Equal("Reminder's Work Anniversary", events[0].Title);
    }

    [Fact]
    public async Task GetCalendarEventsAsyncWithPartialContactExcludesTheirEvents()
    {
        Guid partialContactId = Guid.NewGuid();
        _context.Contacts!.Add(new Contact { Id = partialContactId, FirstName = "Ghost", LastName = "User", IsPartial = true });

        DateOnly today = DateOnly.FromDateTime(DateTime.Today);

        _context.SignificantDates!.Add(new SignificantDate
        {
            Id = Guid.NewGuid(),
            ContactId = partialContactId,
            Title = SignificantDateTitles.Birthday,
            EventDate = today.AddDays(5),
            RecurrenceType = Core.Enumerations.RecurrenceType.Annual,
            IsActive = true
        });

        await _context.SaveChangesAsync();

        List<CalendarEventDto> events = await _service.GetCalendarEventsAsync();

        Assert.Empty(events);
    }

    [Fact]
    public async Task GetCalendarEventsAsyncWithDeceasedContactExcludesTheirEvents()
    {
        Guid deceasedContactId = Guid.NewGuid();
        _context.Contacts!.Add(new Contact
        {
            Id = deceasedContactId,
            FirstName = "Memorial",
            LastName = "Soul",
            IsDeceased = true,
            DateOfDeath = new DateOnly(2024, 1, 15)
        });

        DateOnly today = DateOnly.FromDateTime(DateTime.Today);

        _context.SignificantDates!.Add(new SignificantDate
        {
            Id = Guid.NewGuid(),
            ContactId = deceasedContactId,
            Title = SignificantDateTitles.Birthday,
            EventDate = today.AddDays(5),
            RecurrenceType = Core.Enumerations.RecurrenceType.Annual,
            IsActive = true
        });

        await _context.SaveChangesAsync();

        List<CalendarEventDto> events = await _service.GetCalendarEventsAsync();

        Assert.Empty(events);
    }
}
