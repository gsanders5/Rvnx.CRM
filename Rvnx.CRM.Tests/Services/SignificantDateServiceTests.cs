using Microsoft.EntityFrameworkCore;
using Moq;
using Rvnx.CRM.Core.Constants;
using Rvnx.CRM.Core.DTOs.Calendar;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Core.Models.Dates;
using Rvnx.CRM.Infrastructure.Data;
using Rvnx.CRM.Infrastructure.Repositories;
using Rvnx.CRM.Infrastructure.Services;

namespace Rvnx.CRM.Tests.Services;

public class SignificantDateServiceTests : IDisposable
{
    private readonly CRMDbContext _context;
    private readonly Repository _repository;
    private readonly SignificantDateService _service;

    public SignificantDateServiceTests()
    {
        var options = new DbContextOptionsBuilder<CRMDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var mockCurrentUserService = new Mock<ICurrentUserService>();
        mockCurrentUserService.Setup(s => s.UserId).Returns(Guid.NewGuid());
        mockCurrentUserService.Setup(s => s.UserName).Returns("test-user");

        _context = new CRMDbContext(options, mockCurrentUserService.Object);
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
        var contactId1 = Guid.NewGuid();
        var contactId2 = Guid.NewGuid();

        _context.Contacts!.AddRange(
            new Contact { Id = contactId1, FirstName = "John", LastName = "Doe" },
            new Contact { Id = contactId2, FirstName = "Jane", LastName = "Smith" }
        );

        var today = DateOnly.FromDateTime(DateTime.Today);

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
        var events = await _service.GetCalendarEventsAsync();

        // Assert
        Assert.NotNull(events);
        Assert.Equal(2, events.Count);

        var birthdayEvent = events.FirstOrDefault(e => e.Title.Contains("Birthday"));
        Assert.NotNull(birthdayEvent);
        Assert.Equal("John's Birthday", birthdayEvent.Title);
        Assert.Equal(CalendarColors.Birthday, birthdayEvent.Color);
        Assert.Equal(contactId1, birthdayEvent.ContactId);

        var anniversaryEvent = events.FirstOrDefault(e => e.Title.Contains("Anniversary"));
        Assert.NotNull(anniversaryEvent);
        Assert.Equal("Jane's Anniversary", anniversaryEvent.Title);
        Assert.Equal(CalendarColors.SignificantDate, anniversaryEvent.Color);
        Assert.Equal(contactId2, anniversaryEvent.ContactId);
    }

    [Fact]
    public async Task GetCalendarEventsAsyncWhenNoActiveDatesReturnsEmptyList()
    {
        var contactId = Guid.NewGuid();
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

        var events = await _service.GetCalendarEventsAsync();

        Assert.Empty(events);
    }

    [Fact]
    public async Task GetCalendarEventsAsyncIncludesCurrentYearOccurrenceWhenApplicable()
    {
        var contactId = Guid.NewGuid();
        _context.Contacts!.Add(new Contact { Id = contactId, FirstName = "Alice", LastName = "Wonderland" });

        var today = DateOnly.FromDateTime(DateTime.Today);
        var eventDate = today.AddDays(-10); // Event was 10 days ago

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

        var events = await _service.GetCalendarEventsAsync();

        // We expect two events: the next occurrence (next year) and the current year's occurrence
        Assert.Equal(2, events.Count);
        Assert.All(events, e => Assert.Equal("Alice's Annual Meeting", e.Title));
        Assert.Contains(events, e => e.Start == eventDate.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture));
        Assert.Contains(events, e => e.Start == eventDate.AddYears(1).ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture));
    }
}