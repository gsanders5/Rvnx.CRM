using Microsoft.Extensions.Logging;
using Moq;
using Rvnx.CRM.Core.DTOs.Dashboard;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models.Base;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Core.Models.Dates;
using Rvnx.CRM.Core.Services;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using static Rvnx.CRM.Core.Services.DashboardService;

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

    private void SetupContactSummaries(List<ContactSummary> summaries)
    {
        _repositoryMock
            .Setup(r => r.ListProjectedAsync<Contact, ContactSummary>(
                It.IsAny<Expression<Func<Contact, bool>>>(),
                It.IsAny<Expression<Func<Contact, ContactSummary>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(summaries);
    }

    private void SetupSignificantDates(List<SignificantDate> dates)
    {
        _repositoryMock
            .Setup(r => r.ListAsNoTrackingAsync<SignificantDate>(
                It.IsAny<Expression<Func<SignificantDate, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(dates);
    }

    private void SetupRelationships(List<(Guid EntityId, Guid RelatedEntityId)> relationships)
    {
        _repositoryMock
            .Setup(r => r.ListProjectedAsync<Relationship, (Guid EntityId, Guid RelatedEntityId)>(
                It.IsAny<Expression<Func<Relationship, bool>>>(),
                It.IsAny<Expression<Func<Relationship, (Guid EntityId, Guid RelatedEntityId)>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(relationships);
    }

    private void SetupAttachments(List<(Guid ContactId, Guid AttachmentId)> attachments)
    {
        _repositoryMock
            .Setup(r => r.ListProjectedAsync<Attachment, (Guid ContactId, Guid AttachmentId)>(
                It.IsAny<Expression<Func<Attachment, bool>>>(),
                It.IsAny<Expression<Func<Attachment, (Guid, Guid)>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(attachments);
    }

    private async Task<DashboardDto> GetResultForSingleContactEvent(DateOnly eventDate)
    {
        Guid contactId = Guid.NewGuid();
        DateTime now = DateTime.UtcNow;

        SetupContactSummaries([new ContactSummary(contactId, "Test", "User", null, now, now)]);
        SetupAttachments([]);
        SetupSignificantDates([
            new SignificantDate
            {
                ContactId = contactId,
                Title = "Event",
                EventDate = eventDate,
                IsActive = true
            }
        ]);
        SetupRelationships([]);

        return await _service.GetDashboardDataAsync();
    }


    [Fact]
    [SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores",
        Justification = "Test names can contain underscores for readability.")]
    public async Task GetDashboardDataAsync_ReturnsAggregatedData()
    {
        Guid contactId1 = Guid.NewGuid();
        Guid contactId2 = Guid.NewGuid();
        DateTime now = DateTime.UtcNow;

        SetupContactSummaries([
            new ContactSummary(contactId1, "Alice", "Smith", "Female", now, now),
            new ContactSummary(contactId2, "Bob",   "Jones", "Male",   now, now)
        ]);

        DateTime today = DateTime.Today;
        SetupSignificantDates([
            new SignificantDate
            {
                ContactId = contactId1,
                Title = "Birthday",
                EventDate = DateOnly.FromDateTime(today.AddYears(-30).AddDays(2)),
                IsActive = true
            },
            new SignificantDate
            {
                ContactId = contactId2,
                Title = "Anniversary",
                EventDate = DateOnly.FromDateTime(today.AddYears(-5).AddDays(10)),
                IsActive = true
            }
        ]);

        SetupRelationships([(contactId1, contactId2)]);

        Guid attachmentId = Guid.NewGuid();
        SetupAttachments([(contactId1, attachmentId)]);

        _loggerMock.Setup(x => x.IsEnabled(LogLevel.Warning)).Returns(true);

        DashboardDto result = await _service.GetDashboardDataAsync();

        Assert.NotNull(result);

        Assert.Equal(2, result.UpcomingEvents.Count);
        Assert.Equal("Alice's Birthday", result.UpcomingEvents[0].Title);
        Assert.Equal("Bob's Anniversary", result.UpcomingEvents[1].Title);

        Assert.Equal(2, result.GraphNodes.Count);
        Assert.Contains(result.GraphNodes,
            n => n.Id == contactId1.ToString() && n.Name == "Alice Smith" &&
                 n.PhotoUrl == $"/Attachments/Thumbnail/{attachmentId}?maxWidth=80&maxHeight=80");
        Assert.Contains(result.GraphNodes,
            n => n.Id == contactId2.ToString() && n.Name == "Bob Jones" && n.PhotoUrl == null);

        Assert.Single(result.GraphLinks);
        Assert.Equal(contactId1.ToString(), result.GraphLinks[0].Source);
        Assert.Equal(contactId2.ToString(), result.GraphLinks[0].Target);
    }

    [Fact]
    [SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores",
        Justification = "Test names can contain underscores for readability.")]
    public async Task GetDashboardDataAsync_ZeroContacts_ReturnsEmptyData()
    {
        SetupContactSummaries([]);
        SetupSignificantDates([]);
        SetupRelationships([]);

        DashboardDto result = await _service.GetDashboardDataAsync();

        Assert.NotNull(result);
        Assert.Empty(result.GraphNodes);
        Assert.Empty(result.GraphLinks);
        Assert.Empty(result.UpcomingEvents);

        // Attachment fetch must be skipped entirely when there are no contacts.
        _repositoryMock.Verify(r => r.ListProjectedAsync<Attachment, (Guid ContactId, Guid AttachmentId)>(
            It.IsAny<Expression<Func<Attachment, bool>>>(),
            It.IsAny<Expression<Func<Attachment, (Guid, Guid)>>>(),
            It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    [SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores",
        Justification = "Test names can contain underscores for readability.")]
    public async Task GetDashboardDataAsync_NoSignificantDates_ReturnsEmptyUpcomingEvents()
    {
        Guid contactId = Guid.NewGuid();
        DateTime now = DateTime.UtcNow;

        SetupContactSummaries([new ContactSummary(contactId, "Jane", "Doe", "Female", now, now)]);
        SetupAttachments([]);
        SetupSignificantDates([]);
        SetupRelationships([]);

        DashboardDto result = await _service.GetDashboardDataAsync();

        Assert.NotNull(result);
        Assert.Empty(result.UpcomingEvents);
        Assert.Single(result.GraphNodes);
    }

    [Fact]
    [SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores",
        Justification = "Test names can contain underscores for readability.")]
    public async Task GetDashboardDataAsync_ExceedsMaxEventsToProcess_LogsWarning()
    {
        Guid contactId = Guid.NewGuid();
        DateTime now = DateTime.UtcNow;

        SetupContactSummaries([new ContactSummary(contactId, "Max", "Events", "Male", now, now)]);
        SetupAttachments([]);

        // 501 dates exceeds MaxEventsToProcess (500) and triggers the warning log.
        SetupSignificantDates(Enumerable.Range(0, 501).Select(i => new SignificantDate
        {
            ContactId = contactId,
            Title = $"Event {i}",
            EventDate = DateOnly.FromDateTime(DateTime.Today.AddDays(i + 1)),
            IsActive = true
        }).ToList());

        SetupRelationships([]);

        _loggerMock.Setup(x => x.IsEnabled(LogLevel.Warning)).Returns(true);

        DashboardDto result = await _service.GetDashboardDataAsync();

        Assert.NotNull(result);
        Assert.Equal(5, result.UpcomingEvents.Count);
    }

    [Fact]
    [SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores",
        Justification = "Test names can contain underscores for readability.")]
    public async Task GetDashboardDataAsync_EventIsToday_ReturnsToday()
    {
        DashboardDto result = await GetResultForSingleContactEvent(
            DateOnly.FromDateTime(DateTime.Today));

        Assert.Single(result.UpcomingEvents);
        Assert.Equal("Today", result.UpcomingEvents[0].TimeUntil);
    }

    [Fact]
    [SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores",
        Justification = "Test names can contain underscores for readability.")]
    public async Task GetDashboardDataAsync_EventIsTomorrow_ReturnsTomorrow()
    {
        DashboardDto result = await GetResultForSingleContactEvent(
            DateOnly.FromDateTime(DateTime.Today.AddDays(1)));

        Assert.Single(result.UpcomingEvents);
        Assert.Equal("Tomorrow", result.UpcomingEvents[0].TimeUntil);
    }

    [Fact]
    [SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores",
        Justification = "Test names can contain underscores for readability.")]
    public async Task GetDashboardDataAsync_EventIsOverdue_ReturnsOverdue()
    {
        DashboardDto result = await GetResultForSingleContactEvent(
            DateOnly.FromDateTime(DateTime.Today.AddDays(-1)));

        Assert.Single(result.UpcomingEvents);
        Assert.Equal("Overdue", result.UpcomingEvents[0].TimeUntil);
    }

    [Fact]
    [SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores",
        Justification = "Test names can contain underscores for readability.")]
    public async Task GetDashboardDataAsync_EventIsInFiveDays_ReturnsInFiveDays()
    {
        DashboardDto result = await GetResultForSingleContactEvent(
            DateOnly.FromDateTime(DateTime.Today.AddDays(5)));

        Assert.Single(result.UpcomingEvents);
        Assert.Equal("In 5 days", result.UpcomingEvents[0].TimeUntil);
    }

    [Fact]
    [SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores",
        Justification = "Test names can contain underscores for readability.")]
    public async Task GetDashboardDataAsync_EventIsInOneWeek_ReturnsInOneWeek()
    {
        DashboardDto result = await GetResultForSingleContactEvent(
            DateOnly.FromDateTime(DateTime.Today.AddDays(7)));

        Assert.Single(result.UpcomingEvents);
        Assert.Equal("In 1 week", result.UpcomingEvents[0].TimeUntil);
    }

    [Fact]
    [SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores",
        Justification = "Test names can contain underscores for readability.")]
    public async Task GetDashboardDataAsync_EventIsInTwoWeeks_ReturnsInTwoWeeks()
    {
        DashboardDto result = await GetResultForSingleContactEvent(
            DateOnly.FromDateTime(DateTime.Today.AddDays(15)));

        Assert.Single(result.UpcomingEvents);
        Assert.Equal("In 2 weeks", result.UpcomingEvents[0].TimeUntil);
    }
}