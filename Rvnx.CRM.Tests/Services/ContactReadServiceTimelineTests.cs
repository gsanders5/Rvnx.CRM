using Moq;
using Rvnx.CRM.Core.Constants;
using Rvnx.CRM.Core.DTOs.Contact;
using Rvnx.CRM.Core.Enumerations;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models.Base;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Core.Models.Dates;
using Rvnx.CRM.Core.Services;
using System.Linq.Expressions;

namespace Rvnx.CRM.Tests.Services
{
    public class ContactReadServiceTimelineTests
    {
        private readonly Mock<IRepository> _repositoryMock;
        private readonly ContactReadService _service;

        public ContactReadServiceTimelineTests()
        {
            _repositoryMock = new Mock<IRepository>();
            _service = new ContactReadService(_repositoryMock.Object);
        }

        [Fact]
        public async Task GetContactDetailsAsyncPopulatesTimelineWithMixedContentSortedDescending()
        {
            // Arrange
            Guid contactId = Guid.NewGuid();
            DateTime now = DateTime.Now;

            Contact contact = new() { Id = contactId, FirstName = "Timeline", LastName = "Test" };

            // 1. Note (Recent)
            Note note = new()
            {
                Id = Guid.NewGuid(),
                ContactId = contactId,
                Title = "Recent Note",
                Value = "Content",
                CreatedDate = now.AddDays(-1)
            };
            contact.Notes.Add(note);

            // 2. Completed Reminder (Older)
            Reminder completedReminder = new()
            {
                Id = Guid.NewGuid(),
                ContactId = contactId,
                Title = "Done Task",
                DueDate = now.AddDays(-5),
                IsCompleted = true
            };
            contact.Reminders.Add(completedReminder);

            // 3. Past Significant Date (Oldest)
            SignificantDate pastDate = new()
            {
                Id = Guid.NewGuid(),
                ContactId = contactId,
                Title = "Anniversary",
                Date = now.AddYears(-1)
            };
            contact.SignificantDates.Add(pastDate);

            _repositoryMock.Setup(r => r.ListAsNoTrackingAsync<Contact>(
                It.IsAny<Expression<Func<Contact, bool>>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<string[]>()))
                .ReturnsAsync([contact]);

            // Mock Relationships to be empty to isolate timeline logic
            _repositoryMock.Setup(r => r.ListAsNoTrackingAsync<Relationship>(It.IsAny<Expression<Func<Relationship, bool>>>(), default))
                .ReturnsAsync([]);

            // Act
            ContactDetailDto? result = await _service.GetContactDetailsAsync(contactId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.Timeline.Count());

            List<InteractionDto> timeline = result.Timeline.ToList();

            // Item 1: Note (-1 day)
            Assert.Equal("Note", timeline[0].Type);
            Assert.Equal("Recent Note", timeline[0].Title);
            Assert.Equal(note.CreatedDate, timeline[0].Date);

            // Item 2: Reminder (-5 days)
            Assert.Equal("Reminder", timeline[1].Type);
            Assert.Equal("Done Task", timeline[1].Title);
            Assert.Equal(completedReminder.DueDate, timeline[1].Date);

            // Item 3: Date (-1 year)
            Assert.Equal("Date", timeline[2].Type);
            Assert.Equal("Anniversary", timeline[2].Title);
            Assert.Equal(pastDate.Date, timeline[2].Date);
        }

        [Fact]
        public async Task GetContactDetailsAsyncTimelineFiltersOutPendingRemindersAndFutureDates()
        {
            // Arrange
            Guid contactId = Guid.NewGuid();
            DateTime now = DateTime.Now;

            Contact contact = new() { Id = contactId, FirstName = "Filter", LastName = "Test" };

            // 1. Pending Reminder (Should be excluded)
            Reminder pendingReminder = new()
            {
                Id = Guid.NewGuid(),
                ContactId = contactId,
                Title = "Pending Task",
                DueDate = now.AddDays(-2), // Even if past due, if not completed, it's not history
                IsCompleted = false
            };
            contact.Reminders.Add(pendingReminder);

            // 2. Future Significant Date (Should be excluded)
            SignificantDate futureDate = new()
            {
                Id = Guid.NewGuid(),
                ContactId = contactId,
                Title = "Next Birthday",
                Date = now.AddDays(10)
            };
            contact.SignificantDates.Add(futureDate);

            // 3. Valid Note (Should be included)
            Note note = new()
            {
                Id = Guid.NewGuid(),
                ContactId = contactId,
                Title = "Valid Note",
                CreatedDate = now
            };
            contact.Notes.Add(note);

            _repositoryMock.Setup(r => r.ListAsNoTrackingAsync<Contact>(
                It.IsAny<Expression<Func<Contact, bool>>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<string[]>()))
                .ReturnsAsync([contact]);

            _repositoryMock.Setup(r => r.ListAsNoTrackingAsync<Relationship>(It.IsAny<Expression<Func<Relationship, bool>>>(), default))
                .ReturnsAsync([]);

            // Act
            ContactDetailDto? result = await _service.GetContactDetailsAsync(contactId);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Timeline);
            Assert.Equal("Note", result.Timeline.First().Type);
        }

        [Fact]
        public async Task GetContactDetailsAsyncTimelineHandlesEmptyListsGracefully()
        {
            // Arrange
            Guid contactId = Guid.NewGuid();
            Contact contact = new() { Id = contactId, FirstName = "Empty", LastName = "Test" };

            // No notes, reminders, or dates added

            _repositoryMock.Setup(r => r.ListAsNoTrackingAsync<Contact>(
                It.IsAny<Expression<Func<Contact, bool>>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<string[]>()))
                .ReturnsAsync([contact]);

            _repositoryMock.Setup(r => r.ListAsNoTrackingAsync<Relationship>(It.IsAny<Expression<Func<Relationship, bool>>>(), default))
                .ReturnsAsync([]);

            // Act
            ContactDetailDto? result = await _service.GetContactDetailsAsync(contactId);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.Timeline);
        }
    }
}
