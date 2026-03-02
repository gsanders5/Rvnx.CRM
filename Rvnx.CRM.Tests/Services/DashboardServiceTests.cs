using Microsoft.Extensions.Logging;
using Moq;
using Rvnx.CRM.Core.DTOs.Dashboard;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models.Base;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Core.Models.Dates;
using Rvnx.CRM.Core.Services;
using System.Linq.Expressions;
using System.Diagnostics.CodeAnalysis;

namespace Rvnx.CRM.Tests.Services;

public class DashboardServiceTests
{
    private readonly Mock<IRepository> _repositoryMock;
    private readonly Mock<ILogger<DashboardService>> _loggerMock;
    private readonly DashboardService _service;

    public DashboardServiceTests()
    {
        _repositoryMock = new Mock<IRepository>();
        _loggerMock = new Mock<ILogger<DashboardService>>();
        _service = new DashboardService(_repositoryMock.Object, _loggerMock.Object);
    }

    [Fact]
    [SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "Test names can contain underscores for readability.")]
    public async Task GetDashboardDataAsync_ReturnsAggregatedData()
    {
        // Arrange
        Guid contactId1 = Guid.NewGuid();
        Guid contactId2 = Guid.NewGuid();

        List<Contact> contacts =
        [
            new Contact { Id = contactId1, FirstName = "Alice", LastName = "Smith", Gender = "Female", IsHidden = false },
            new Contact { Id = contactId2, FirstName = "Bob", LastName = "Jones", Gender = "Male", IsHidden = false }
        ];

        _repositoryMock.Setup(r => r.ListAsNoTrackingAsync<Contact>(
            It.IsAny<Expression<Func<Contact, bool>>>(),
            It.IsAny<CancellationToken>(),
            It.IsAny<string[]>()))
            .ReturnsAsync(contacts);


        // Setup significant dates
        DateTime today = DateTime.Today;
        List<SignificantDate> dates =
        [
            new SignificantDate
            {
                ContactId = contactId1,
                Title = "Birthday",
                EventDate = DateOnly.FromDateTime(today.AddYears(-30).AddDays(2)), // in 2 days
                IsActive = true
            },
            new SignificantDate
            {
                ContactId = contactId2,
                Title = "Anniversary",
                EventDate = DateOnly.FromDateTime(today.AddYears(-5).AddDays(10)), // in 10 days
                IsActive = true
            }
        ];

        _repositoryMock.Setup(r => r.ListAsNoTrackingAsync<SignificantDate>(
            It.IsAny<Expression<Func<SignificantDate, bool>>>(),
            It.IsAny<CancellationToken>(),
            It.IsAny<string[]>()))
            .ReturnsAsync(dates);

        // Setup relationships
        List<Relationship> relationships =
        [
            new Relationship { EntityId = contactId1, RelatedEntityId = contactId2, EntityType = "Person" }
        ];

        _repositoryMock.Setup(r => r.ListAsNoTrackingAsync<Relationship>(
            It.IsAny<Expression<Func<Relationship, bool>>>(),
            It.IsAny<CancellationToken>(),
            It.IsAny<string[]>()))
            .ReturnsAsync(relationships);

        // Setup attachment map
        Guid attachmentId = Guid.NewGuid();
        _repositoryMock.Setup(r => r.ListProjectedAsync<Attachment, (Guid ContactId, Guid AttachmentId)>(
            It.IsAny<Expression<Func<Attachment, bool>>>(),
            It.IsAny<Expression<Func<Attachment, (Guid, Guid)>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync([(contactId1, attachmentId)]);

        // Ensure logger allows logging for warning
        _loggerMock.Setup(x => x.IsEnabled(LogLevel.Warning)).Returns(true);

        // Act
        DashboardDto result = await _service.GetDashboardDataAsync();

        // Assert
        Assert.NotNull(result);

        // Check UpcomingEvents
        Assert.Equal(2, result.UpcomingEvents.Count);
        // Queue pops earliest first.
        Assert.Equal("Alice's Birthday", result.UpcomingEvents[0].Title);
        Assert.Equal("Bob's Anniversary", result.UpcomingEvents[1].Title);

        // Check GraphNodes
        Assert.Equal(2, result.GraphNodes.Count);
        Assert.Contains(result.GraphNodes, n => n.Id == contactId1.ToString() && n.Name == "Alice Smith" && n.PhotoUrl == $"/Attachments/View/{attachmentId}");
        Assert.Contains(result.GraphNodes, n => n.Id == contactId2.ToString() && n.Name == "Bob Jones" && n.PhotoUrl == null);

        // Check GraphLinks
        Assert.Single(result.GraphLinks);
        Assert.Equal(contactId1.ToString(), result.GraphLinks[0].Source);
        Assert.Equal(contactId2.ToString(), result.GraphLinks[0].Target);
    }

    [Fact]
    [SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "Test names can contain underscores for readability.")]
    public async Task GetDashboardDataAsync_ZeroContacts_ReturnsEmptyData()
    {
        // Arrange
        _repositoryMock.Setup(r => r.ListAsNoTrackingAsync<Contact>(
            It.IsAny<Expression<Func<Contact, bool>>>(),
            It.IsAny<CancellationToken>(),
            It.IsAny<string[]>()))
            .ReturnsAsync([]);

        _repositoryMock.Setup(r => r.ListAsNoTrackingAsync<SignificantDate>(
            It.IsAny<Expression<Func<SignificantDate, bool>>>(),
            It.IsAny<CancellationToken>(),
            It.IsAny<string[]>()))
            .ReturnsAsync([]);

        _repositoryMock.Setup(r => r.ListAsNoTrackingAsync<Relationship>(
            It.IsAny<Expression<Func<Relationship, bool>>>(),
            It.IsAny<CancellationToken>(),
            It.IsAny<string[]>()))
            .ReturnsAsync([]);

        // Act
        DashboardDto result = await _service.GetDashboardDataAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.GraphNodes);
        Assert.Empty(result.GraphLinks);
        Assert.Empty(result.UpcomingEvents);

        // Ensure ListProjectedAsync is never called for attachments if no contacts exist
        _repositoryMock.Verify(r => r.ListProjectedAsync<Attachment, (Guid ContactId, Guid AttachmentId)>(
            It.IsAny<Expression<Func<Attachment, bool>>>(),
            It.IsAny<Expression<Func<Attachment, (Guid, Guid)>>>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "Test names can contain underscores for readability.")]
    public async Task GetDashboardDataAsync_NoSignificantDates_ReturnsEmptyUpcomingEvents()
    {
        // Arrange
        Guid contactId = Guid.NewGuid();
        List<Contact> contacts = [new Contact { Id = contactId, FirstName = "Jane", LastName = "Doe", Gender = "Female" }];

        _repositoryMock.Setup(r => r.ListAsNoTrackingAsync<Contact>(
            It.IsAny<Expression<Func<Contact, bool>>>(),
            It.IsAny<CancellationToken>(),
            It.IsAny<string[]>()))
            .ReturnsAsync(contacts);

        _repositoryMock.Setup(r => r.ListProjectedAsync<Attachment, (Guid ContactId, Guid AttachmentId)>(
            It.IsAny<Expression<Func<Attachment, bool>>>(),
            It.IsAny<Expression<Func<Attachment, (Guid, Guid)>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        // No dates returned
        _repositoryMock.Setup(r => r.ListAsNoTrackingAsync<SignificantDate>(
            It.IsAny<Expression<Func<SignificantDate, bool>>>(),
            It.IsAny<CancellationToken>(),
            It.IsAny<string[]>()))
            .ReturnsAsync([]);

        _repositoryMock.Setup(r => r.ListAsNoTrackingAsync<Relationship>(
            It.IsAny<Expression<Func<Relationship, bool>>>(),
            It.IsAny<CancellationToken>(),
            It.IsAny<string[]>()))
            .ReturnsAsync([]);

        // Act
        DashboardDto result = await _service.GetDashboardDataAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.UpcomingEvents);
        Assert.Single(result.GraphNodes);
    }

    [Fact]
    [SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "Test names can contain underscores for readability.")]
    public async Task GetDashboardDataAsync_ExceedsMaxEventsToProcess_LogsWarning()
    {
        // Arrange
        Guid contactId = Guid.NewGuid();
        List<Contact> contacts = [new Contact { Id = contactId, FirstName = "Max", LastName = "Events", Gender = "Male" }];

        _repositoryMock.Setup(r => r.ListAsNoTrackingAsync<Contact>(
            It.IsAny<Expression<Func<Contact, bool>>>(),
            It.IsAny<CancellationToken>(),
            It.IsAny<string[]>()))
            .ReturnsAsync(contacts);

        _repositoryMock.Setup(r => r.ListProjectedAsync<Attachment, (Guid ContactId, Guid AttachmentId)>(
            It.IsAny<Expression<Func<Attachment, bool>>>(),
            It.IsAny<Expression<Func<Attachment, (Guid, Guid)>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        // Create 501 dates to trigger the MaxEventsToProcess (500) limit logic
        // We must include the contact in contactDict for it to count as processed
        List<SignificantDate> dates = Enumerable.Range(0, 501).Select(i => new SignificantDate
        {
            ContactId = contactId,
            Title = $"Event {i}",
            EventDate = DateOnly.FromDateTime(DateTime.Today.AddDays(i + 1)),
            IsActive = true
        }).ToList();

        // Need contact to not be hidden to be returned
        contacts[0].IsHidden = false;

        _repositoryMock.Setup(r => r.ListAsNoTrackingAsync<SignificantDate>(
            It.IsAny<Expression<Func<SignificantDate, bool>>>(),
            It.IsAny<CancellationToken>(),
            It.IsAny<string[]>()))
            .ReturnsAsync(dates);

        _repositoryMock.Setup(r => r.ListAsNoTrackingAsync<Relationship>(
            It.IsAny<Expression<Func<Relationship, bool>>>(),
            It.IsAny<CancellationToken>(),
            It.IsAny<string[]>()))
            .ReturnsAsync([]);

        // Act
        DashboardDto result = await _service.GetDashboardDataAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(5, result.UpcomingEvents.Count); // Should still limit to MaxUpcomingEvents (5)

    }

    [Fact]
    [SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "Test names can contain underscores for readability.")]
    public async Task GetDashboardDataAsync_EventIsToday_ReturnsToday()
    {
        // Arrange
        Guid contactId = Guid.NewGuid();
        List<Contact> contacts = [new Contact { Id = contactId, FirstName = "Test", LastName = "User" }];

        _repositoryMock.Setup(r => r.ListAsNoTrackingAsync<Contact>(It.IsAny<Expression<Func<Contact, bool>>>(), It.IsAny<CancellationToken>(), It.IsAny<string[]>()))
            .ReturnsAsync(contacts);

        _repositoryMock.Setup(r => r.ListProjectedAsync<Attachment, (Guid ContactId, Guid AttachmentId)>(It.IsAny<Expression<Func<Attachment, bool>>>(), It.IsAny<Expression<Func<Attachment, (Guid, Guid)>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        List<SignificantDate> dates = [new SignificantDate { ContactId = contactId, Title = "Event", EventDate = DateOnly.FromDateTime(DateTime.Today), IsActive = true }];
        _repositoryMock.Setup(r => r.ListAsNoTrackingAsync<SignificantDate>(It.IsAny<Expression<Func<SignificantDate, bool>>>(), It.IsAny<CancellationToken>(), It.IsAny<string[]>()))
            .ReturnsAsync(dates);

        _repositoryMock.Setup(r => r.ListAsNoTrackingAsync<Relationship>(It.IsAny<Expression<Func<Relationship, bool>>>(), It.IsAny<CancellationToken>(), It.IsAny<string[]>()))
            .ReturnsAsync([]);

        // Act
        DashboardDto result = await _service.GetDashboardDataAsync();

        // Assert
        Assert.Single(result.UpcomingEvents);
        Assert.Equal("Today", result.UpcomingEvents[0].TimeUntil);
    }

    [Fact]
    [SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "Test names can contain underscores for readability.")]
    public async Task GetDashboardDataAsync_EventIsTomorrow_ReturnsTomorrow()
    {
        // Arrange
        Guid contactId = Guid.NewGuid();
        List<Contact> contacts = [new Contact { Id = contactId, FirstName = "Test", LastName = "User" }];

        _repositoryMock.Setup(r => r.ListAsNoTrackingAsync<Contact>(It.IsAny<Expression<Func<Contact, bool>>>(), It.IsAny<CancellationToken>(), It.IsAny<string[]>()))
            .ReturnsAsync(contacts);

        _repositoryMock.Setup(r => r.ListProjectedAsync<Attachment, (Guid ContactId, Guid AttachmentId)>(It.IsAny<Expression<Func<Attachment, bool>>>(), It.IsAny<Expression<Func<Attachment, (Guid, Guid)>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        List<SignificantDate> dates = [new SignificantDate { ContactId = contactId, Title = "Event", EventDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1)), IsActive = true }];
        _repositoryMock.Setup(r => r.ListAsNoTrackingAsync<SignificantDate>(It.IsAny<Expression<Func<SignificantDate, bool>>>(), It.IsAny<CancellationToken>(), It.IsAny<string[]>()))
            .ReturnsAsync(dates);

        _repositoryMock.Setup(r => r.ListAsNoTrackingAsync<Relationship>(It.IsAny<Expression<Func<Relationship, bool>>>(), It.IsAny<CancellationToken>(), It.IsAny<string[]>()))
            .ReturnsAsync([]);

        // Act
        DashboardDto result = await _service.GetDashboardDataAsync();

        // Assert
        Assert.Single(result.UpcomingEvents);
        Assert.Equal("Tomorrow", result.UpcomingEvents[0].TimeUntil);
    }

    [Fact]
    [SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "Test names can contain underscores for readability.")]
    public async Task GetDashboardDataAsync_EventIsOverdue_ReturnsOverdue()
    {
        // Arrange
        Guid contactId = Guid.NewGuid();
        List<Contact> contacts = [new Contact { Id = contactId, FirstName = "Test", LastName = "User" }];

        _repositoryMock.Setup(r => r.ListAsNoTrackingAsync<Contact>(It.IsAny<Expression<Func<Contact, bool>>>(), It.IsAny<CancellationToken>(), It.IsAny<string[]>()))
            .ReturnsAsync(contacts);

        _repositoryMock.Setup(r => r.ListProjectedAsync<Attachment, (Guid ContactId, Guid AttachmentId)>(It.IsAny<Expression<Func<Attachment, bool>>>(), It.IsAny<Expression<Func<Attachment, (Guid, Guid)>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        // Dates are calculated using GetNextOccurrence(). For an event to be "overdue", the next occurrence must be in the past.
        // This validates the switch branch logic handling past dates.
        List<SignificantDate> dates = [new SignificantDate { ContactId = contactId, Title = "Event", EventDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-1)), IsActive = true }];

        _repositoryMock.Setup(r => r.ListAsNoTrackingAsync<SignificantDate>(It.IsAny<Expression<Func<SignificantDate, bool>>>(), It.IsAny<CancellationToken>(), It.IsAny<string[]>()))
            .ReturnsAsync(dates);

        _repositoryMock.Setup(r => r.ListAsNoTrackingAsync<Relationship>(It.IsAny<Expression<Func<Relationship, bool>>>(), It.IsAny<CancellationToken>(), It.IsAny<string[]>()))
            .ReturnsAsync([]);

        // Act
        DashboardDto result = await _service.GetDashboardDataAsync();

        // Assert
        Assert.Single(result.UpcomingEvents);
        Assert.Equal("Overdue", result.UpcomingEvents[0].TimeUntil);
    }

    [Fact]
    [SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "Test names can contain underscores for readability.")]
    public async Task GetDashboardDataAsync_EventIsInFiveDays_ReturnsInFiveDays()
    {
        // Arrange
        Guid contactId = Guid.NewGuid();
        List<Contact> contacts = [new Contact { Id = contactId, FirstName = "Test", LastName = "User" }];

        _repositoryMock.Setup(r => r.ListAsNoTrackingAsync<Contact>(It.IsAny<Expression<Func<Contact, bool>>>(), It.IsAny<CancellationToken>(), It.IsAny<string[]>()))
            .ReturnsAsync(contacts);

        _repositoryMock.Setup(r => r.ListProjectedAsync<Attachment, (Guid ContactId, Guid AttachmentId)>(It.IsAny<Expression<Func<Attachment, bool>>>(), It.IsAny<Expression<Func<Attachment, (Guid, Guid)>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        List<SignificantDate> dates = [new SignificantDate { ContactId = contactId, Title = "Event", EventDate = DateOnly.FromDateTime(DateTime.Today.AddDays(5)), IsActive = true }];

        _repositoryMock.Setup(r => r.ListAsNoTrackingAsync<SignificantDate>(It.IsAny<Expression<Func<SignificantDate, bool>>>(), It.IsAny<CancellationToken>(), It.IsAny<string[]>()))
            .ReturnsAsync(dates);

        _repositoryMock.Setup(r => r.ListAsNoTrackingAsync<Relationship>(It.IsAny<Expression<Func<Relationship, bool>>>(), It.IsAny<CancellationToken>(), It.IsAny<string[]>()))
            .ReturnsAsync([]);

        // Act
        DashboardDto result = await _service.GetDashboardDataAsync();

        // Assert
        Assert.Single(result.UpcomingEvents);
        Assert.Equal("In 5 days", result.UpcomingEvents[0].TimeUntil);
    }

    [Fact]
    [SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "Test names can contain underscores for readability.")]
    public async Task GetDashboardDataAsync_EventIsInOneWeek_ReturnsInOneWeek()
    {
        // Arrange
        Guid contactId = Guid.NewGuid();
        List<Contact> contacts = [new Contact { Id = contactId, FirstName = "Test", LastName = "User" }];

        _repositoryMock.Setup(r => r.ListAsNoTrackingAsync<Contact>(It.IsAny<Expression<Func<Contact, bool>>>(), It.IsAny<CancellationToken>(), It.IsAny<string[]>()))
            .ReturnsAsync(contacts);

        _repositoryMock.Setup(r => r.ListProjectedAsync<Attachment, (Guid ContactId, Guid AttachmentId)>(It.IsAny<Expression<Func<Attachment, bool>>>(), It.IsAny<Expression<Func<Attachment, (Guid, Guid)>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        List<SignificantDate> dates = [new SignificantDate { ContactId = contactId, Title = "Event", EventDate = DateOnly.FromDateTime(DateTime.Today.AddDays(7)), IsActive = true }];

        _repositoryMock.Setup(r => r.ListAsNoTrackingAsync<SignificantDate>(It.IsAny<Expression<Func<SignificantDate, bool>>>(), It.IsAny<CancellationToken>(), It.IsAny<string[]>()))
            .ReturnsAsync(dates);

        _repositoryMock.Setup(r => r.ListAsNoTrackingAsync<Relationship>(It.IsAny<Expression<Func<Relationship, bool>>>(), It.IsAny<CancellationToken>(), It.IsAny<string[]>()))
            .ReturnsAsync([]);

        // Act
        DashboardDto result = await _service.GetDashboardDataAsync();

        // Assert
        Assert.Single(result.UpcomingEvents);
        Assert.Equal("In 1 week", result.UpcomingEvents[0].TimeUntil);
    }

    [Fact]
    [SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "Test names can contain underscores for readability.")]
    public async Task GetDashboardDataAsync_EventIsInTwoWeeks_ReturnsInTwoWeeks()
    {
        // Arrange
        Guid contactId = Guid.NewGuid();
        List<Contact> contacts = [new Contact { Id = contactId, FirstName = "Test", LastName = "User" }];

        _repositoryMock.Setup(r => r.ListAsNoTrackingAsync<Contact>(It.IsAny<Expression<Func<Contact, bool>>>(), It.IsAny<CancellationToken>(), It.IsAny<string[]>()))
            .ReturnsAsync(contacts);

        _repositoryMock.Setup(r => r.ListProjectedAsync<Attachment, (Guid ContactId, Guid AttachmentId)>(It.IsAny<Expression<Func<Attachment, bool>>>(), It.IsAny<Expression<Func<Attachment, (Guid, Guid)>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        List<SignificantDate> dates = [new SignificantDate { ContactId = contactId, Title = "Event", EventDate = DateOnly.FromDateTime(DateTime.Today.AddDays(15)), IsActive = true }];

        _repositoryMock.Setup(r => r.ListAsNoTrackingAsync<SignificantDate>(It.IsAny<Expression<Func<SignificantDate, bool>>>(), It.IsAny<CancellationToken>(), It.IsAny<string[]>()))
            .ReturnsAsync(dates);

        _repositoryMock.Setup(r => r.ListAsNoTrackingAsync<Relationship>(It.IsAny<Expression<Func<Relationship, bool>>>(), It.IsAny<CancellationToken>(), It.IsAny<string[]>()))
            .ReturnsAsync([]);

        // Act
        DashboardDto result = await _service.GetDashboardDataAsync();

        // Assert
        Assert.Single(result.UpcomingEvents);
        Assert.Equal("In 2 weeks", result.UpcomingEvents[0].TimeUntil);
    }
}
