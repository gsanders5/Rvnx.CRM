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

        // The open-tasks query runs on every dashboard load; default to empty so tests
        // that don't care about tasks don't need their own setup.
        SetupOpenTasks([]);
    }

    private void SetupOpenTasks(List<(Guid TaskId, Guid? ContactId, string Title, DateOnly DueDate)> tasks)
    {
        _repositoryMock
            .Setup(r => r.ListProjectedAsync<ContactTask, (Guid TaskId, Guid? ContactId, string Title, DateOnly DueDate)>(
                It.IsAny<Expression<Func<ContactTask, bool>>>(),
                It.IsAny<Expression<Func<ContactTask, (Guid, Guid?, string, DateOnly)>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(tasks);
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

        SetupContactSummaries([new ContactSummary(contactId, "Test", "User", null, now, now, false)]);
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
            new ContactSummary(contactId1, "Alice", "Smith", "Female", now, now, false),
            new ContactSummary(contactId2, "Bob",   "Jones", "Male",   now, now, false)
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

        SetupContactSummaries([new ContactSummary(contactId, "Jane", "Doe", "Female", now, now, false)]);
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

        SetupContactSummaries([new ContactSummary(contactId, "Max", "Events", "Male", now, now, false)]);
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

    [Fact]
    public async Task GetDashboardDataAsyncHiddenAndPartialContactsExcluded()
    {
        Guid normalId = Guid.NewGuid();
        DateTime now = DateTime.UtcNow;

        SetupContactSummaries([new ContactSummary(normalId, "Normal", "Contact", null, now, now, false)]);
        SetupAttachments([]);
        SetupSignificantDates([
            new SignificantDate
            {
                ContactId = normalId,
                Title = "Birthday",
                EventDate = DateOnly.FromDateTime(DateTime.Today.AddDays(5)),
                IsActive = true
            }
        ]);
        SetupRelationships([]);

        _repositoryMock
            .Setup(r => r.CountAsync<SignificantDate>(It.IsAny<Expression<Func<SignificantDate, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _repositoryMock
            .Setup(r => r.CountAsync<Contact>(It.IsAny<Expression<Func<Contact, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);

        DashboardDto result = await _service.GetDashboardDataAsync();

        Assert.Single(result.GraphNodes);
        Assert.Equal(normalId.ToString(), result.GraphNodes[0].Id);
        Assert.Equal(1, result.Stats.TotalContacts);
    }

    [Fact]
    public async Task GetDashboardDataAsyncOrphanedSignificantDateFiltered()
    {
        Guid normalId = Guid.NewGuid();
        Guid orphanedContactId = Guid.NewGuid();
        DateTime now = DateTime.UtcNow;

        SetupContactSummaries([new ContactSummary(normalId, "Normal", "Contact", null, now, now, false)]);
        SetupAttachments([]);
        SetupSignificantDates([
            new SignificantDate
            {
                ContactId = orphanedContactId,
                Title = "Birthday",
                EventDate = DateOnly.FromDateTime(DateTime.Today.AddDays(3)),
                IsActive = true
            }
        ]);
        SetupRelationships([]);

        _repositoryMock
            .Setup(r => r.CountAsync<SignificantDate>(It.IsAny<Expression<Func<SignificantDate, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        _repositoryMock
            .Setup(r => r.CountAsync<Contact>(It.IsAny<Expression<Func<Contact, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        DashboardDto result = await _service.GetDashboardDataAsync();

        Assert.Empty(result.UpcomingEvents);
    }

    [Fact]
    [SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores",
        Justification = "Test names can contain underscores for readability.")]
    public async Task GetDashboardDataAsync_CalculatesStatsAndRecentContactsCorrectly()
    {
        Guid contactId1 = Guid.NewGuid();
        Guid contactId2 = Guid.NewGuid();
        Guid contactId3 = Guid.NewGuid();
        DateTime now = DateTime.UtcNow;
        DateTime oldDate = now.AddDays(-10);

        SetupContactSummaries([
            new ContactSummary(contactId1, "Alice", "Smith", "Female", now, now, false), // New contact
            new ContactSummary(contactId2, "Bob", "Jones", "Male", oldDate, oldDate, false), // Old contact
            new ContactSummary(contactId3, "Charlie", "Brown", "Male", oldDate, now.AddDays(-1), false) // Old contact, recently changed
        ]);

        SetupAttachments([]);
        SetupSignificantDates([]);

        // Alice and Bob have a relationship. Charlie has no relationships.
        SetupRelationships([(contactId1, contactId2)]);

        // Set up mock counts for birthday and hidden contacts
        _repositoryMock
            .Setup(r => r.CountAsync<SignificantDate>(It.IsAny<Expression<Func<SignificantDate, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1); // 1 birthday
        _repositoryMock
            .Setup(r => r.CountAsync<Contact>(It.IsAny<Expression<Func<Contact, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(2); // 2 hidden contacts

        DashboardDto result = await _service.GetDashboardDataAsync();

        Assert.NotNull(result);

        Assert.NotNull(result.Stats);
        Assert.Equal(3, result.Stats.TotalContacts);
        Assert.Equal(1, result.Stats.ContactsWithBirthday);
        Assert.Equal(2, result.Stats.ContactsWithRelationships); // Alice and Bob
        Assert.Equal(2, result.Stats.ContactsHidden);

        Assert.NotNull(result.RecentContacts);
        Assert.Equal(3, result.RecentContacts.Count);
        Assert.Equal(contactId1, result.RecentContacts[0].Id); // Alice (LastChangedDate = now)
        Assert.Equal(contactId3, result.RecentContacts[1].Id); // Charlie (LastChangedDate = now - 1 day)
        Assert.Equal(contactId2, result.RecentContacts[2].Id); // Bob (LastChangedDate = now - 10 days)

        Assert.True(result.RecentContacts[0].IsNew);
        Assert.False(result.RecentContacts[1].IsNew);
        Assert.False(result.RecentContacts[2].IsNew);
    }

    [Fact]
    public async Task GetDashboardDataAsyncSuppressesUpcomingEventsForDeceasedContact()
    {
        // Arrange — deceased contacts remain in the network graph (structural) but their
        // birthdays/anniversaries should not appear under "upcoming events".
        Guid livingId = Guid.NewGuid();
        Guid deceasedId = Guid.NewGuid();
        DateTime now = DateTime.UtcNow;
        DateOnly soonBirthday = DateOnly.FromDateTime(DateTime.Today.AddDays(3));

        SetupContactSummaries([
            new ContactSummary(livingId, "Alive", "Person", null, now, now, false),
            new ContactSummary(deceasedId, "Late", "Person", null, now, now, true)
        ]);
        SetupAttachments([]);
        SetupSignificantDates([
            new SignificantDate
            {
                ContactId = livingId,
                Title = "Birthday",
                EventDate = soonBirthday,
                IsActive = true
            },
            new SignificantDate
            {
                ContactId = deceasedId,
                Title = "Birthday",
                EventDate = soonBirthday,
                IsActive = true
            }
        ]);
        SetupRelationships([]);

        DashboardDto result = await _service.GetDashboardDataAsync();

        // Assert — deceased contact still appears as a node, but no upcoming event for them.
        Assert.Equal(2, result.GraphNodes.Count);
        Assert.Single(result.UpcomingEvents);
        Assert.Equal(livingId, result.UpcomingEvents[0].RelatedContactId);
    }

    [Fact]
    public async Task GetDashboardDataAsyncExcludesDeceasedContactsFromRecentContacts()
    {
        // The "Recently Modified" widget is a prompt to revisit someone, so deceased
        // contacts stay out of it even when they were the most recently changed.
        Guid livingId = Guid.NewGuid();
        Guid deceasedId = Guid.NewGuid();
        DateTime now = DateTime.UtcNow;

        SetupContactSummaries([
            new ContactSummary(livingId, "Alive", "Person", null, now.AddDays(-30), now.AddDays(-2), false),
            new ContactSummary(deceasedId, "Late", "Person", null, now.AddDays(-30), now, true)
        ]);
        SetupAttachments([]);
        SetupSignificantDates([]);
        SetupRelationships([]);

        DashboardDto result = await _service.GetDashboardDataAsync();

        // Deceased contact still counts as a node, but is absent from Recently Modified.
        Assert.Equal(2, result.GraphNodes.Count);
        Assert.Single(result.RecentContacts);
        Assert.Equal(livingId, result.RecentContacts[0].Id);
    }

    [Fact]
    public async Task GetDashboardDataAsyncCarriesIsDeceasedThroughToGraphNodes()
    {
        // The network graph dims deceased contacts. The flag must round-trip from the
        // ContactSummary projection into the GraphNodeDto consumed by the front-end.
        Guid livingId = Guid.NewGuid();
        Guid deceasedId = Guid.NewGuid();
        DateTime now = DateTime.UtcNow;

        SetupContactSummaries([
            new ContactSummary(livingId, "Alive", "Person", null, now, now, false),
            new ContactSummary(deceasedId, "Late", "Person", null, now, now, true)
        ]);
        SetupAttachments([]);
        SetupSignificantDates([]);
        SetupRelationships([]);

        DashboardDto result = await _service.GetDashboardDataAsync();

        Assert.Equal(2, result.GraphNodes.Count);
        GraphNodeDto livingNode = result.GraphNodes.Single(n => n.Id == livingId.ToString());
        GraphNodeDto deceasedNode = result.GraphNodes.Single(n => n.Id == deceasedId.ToString());
        Assert.False(livingNode.IsDeceased);
        Assert.True(deceasedNode.IsDeceased);
    }

    [Fact]
    [SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "Test names can contain underscores for readability.")]
    public async Task GetDashboardDataAsync_ExcludesHiddenAndPartialContacts_EvaluatesFilterCorrectly()
    {
        // Arrange
        List<Contact> dbContacts = [
            new Contact { Id = Guid.NewGuid(), FirstName = "Normal", IsHidden = false, IsPartial = false },
            new Contact { Id = Guid.NewGuid(), FirstName = "Hidden", IsHidden = true, IsPartial = false },
            new Contact { Id = Guid.NewGuid(), FirstName = "Partial", IsHidden = false, IsPartial = true }
        ];

        Expression<Func<Contact, bool>>? capturedFilter = null;

        _repositoryMock.Setup(r => r.ListProjectedAsync(
            It.IsAny<Expression<Func<Contact, bool>>>(),
            It.IsAny<Expression<Func<Contact, ContactSummary>>>(),
            It.IsAny<CancellationToken>()))
            .Callback<Expression<Func<Contact, bool>>, Expression<Func<Contact, ContactSummary>>, CancellationToken>(
                (filter, projection, ct) => capturedFilter = filter)
            .ReturnsAsync([]);

        // Setup others to avoid null ref exceptions
        _repositoryMock.Setup(r => r.ListAsNoTrackingAsync<SignificantDate>(It.IsAny<Expression<Func<SignificantDate, bool>>>(), It.IsAny<CancellationToken>(), It.IsAny<string[]>())).ReturnsAsync([]);
        _repositoryMock.Setup(r => r.ListProjectedAsync(It.IsAny<Expression<Func<Relationship, bool>>>(), It.IsAny<Expression<Func<Relationship, (Guid ContactId, Guid RelatedContactId)>>>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _repositoryMock.Setup(r => r.ListProjectedAsync(It.IsAny<Expression<Func<ContactTask, bool>>>(), It.IsAny<Expression<Func<ContactTask, (Guid, Guid?, string, DateOnly)>>>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);

        // Act
        await _service.GetDashboardDataAsync();

        // Assert
        Assert.NotNull(capturedFilter);
        Func<Contact, bool> filterFunc = capturedFilter.Compile();

        Assert.True(filterFunc(dbContacts[0])); // Normal contact is included
        Assert.False(filterFunc(dbContacts[1])); // Hidden contact is excluded
        Assert.False(filterFunc(dbContacts[2])); // Partial contact is excluded
    }
}
