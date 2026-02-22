using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Core.Models.Dates;
using Rvnx.CRM.Core.Services;
using System.Linq.Expressions;
using Xunit;

namespace Rvnx.CRM.Tests.Services
{
    public class DashboardServiceTests
    {
        private readonly Mock<IRepository> _repositoryMock;
        private readonly Mock<ILogger<DashboardService>> _loggerMock;
        private readonly DashboardService _dashboardService;

        public DashboardServiceTests()
        {
            _repositoryMock = new Mock<IRepository>();
            _loggerMock = new Mock<ILogger<DashboardService>>();
            _dashboardService = new DashboardService(_repositoryMock.Object, _loggerMock.Object);
        }

        [Fact]
        public async Task GetDashboardDataAsync_FiltersRemindersAtDatabaseLevel()
        {
            // Arrange
            var contacts = new List<Contact>
            {
                new Contact { Id = Guid.NewGuid(), FirstName = "John", LastName = "Doe" }
            };

            var reminders = new List<Reminder>
            {
                new Reminder
                {
                    Id = Guid.NewGuid(),
                    Title = "Active Reminder",
                    DueDate = DateTime.Today.AddDays(1),
                    IsCompleted = false,
                    EventFrequency = TimeSpan.Zero
                },
                new Reminder
                {
                    Id = Guid.NewGuid(),
                    Title = "Completed One-Time Reminder",
                    DueDate = DateTime.Today.AddDays(-1),
                    IsCompleted = true,
                    EventFrequency = TimeSpan.Zero
                },
                new Reminder
                {
                    Id = Guid.NewGuid(),
                    Title = "Completed Recurring Reminder",
                    DueDate = DateTime.Today.AddDays(-1),
                    IsCompleted = true,
                    EventFrequency = TimeSpan.FromDays(7)
                }
            };

            // Setup mocks for other calls
            _repositoryMock.Setup(r => r.ListAsNoTrackingAsync<Contact>(It.IsAny<Expression<Func<Contact, bool>>>(), It.IsAny<CancellationToken>(),It.IsAny<string[]>()))
                .ReturnsAsync(contacts);

            _repositoryMock.Setup(r => r.ListAsNoTrackingAsync<Relationship>(It.IsAny<Expression<Func<Relationship, bool>>>(), It.IsAny<CancellationToken>(), It.IsAny<string[]>()))
                .ReturnsAsync(new List<Relationship>());

            _repositoryMock.Setup(r => r.ListAsNoTrackingAsync<SignificantDate>(It.IsAny<Expression<Func<SignificantDate, bool>>>(), It.IsAny<CancellationToken>(), It.IsAny<string[]>()))
                .ReturnsAsync(new List<SignificantDate>());


            // Setup for the EXPECTED new behavior (with predicate)
            _repositoryMock.Setup(r => r.ListAsNoTrackingAsync<Reminder>(It.IsAny<Expression<Func<Reminder, bool>>>(), It.IsAny<CancellationToken>(), It.IsAny<string[]>()))
                .ReturnsAsync((Expression<Func<Reminder, bool>> predicate, CancellationToken token, string[] includes) =>
                {
                    return reminders.AsQueryable().Where(predicate).ToList();
                });

            // Setup for the OLD behavior (parameterless / skip-take overload)
            _repositoryMock.Setup(r => r.ListAsNoTrackingAsync<Reminder>(It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(reminders);

            // Act
            var result = await _dashboardService.GetDashboardDataAsync();

            // Assert
            // 1. Verify the parameterless overload is NOT called (this fails initially)
            _repositoryMock.Verify(r => r.ListAsNoTrackingAsync<Reminder>(
                It.IsAny<int?>(),
                It.IsAny<int?>(),
                It.IsAny<CancellationToken>()), Times.Never, "Should NOT call parameterless ListAsNoTrackingAsync (fetching all records)");

            // 2. Verify the predicate overload IS called
            _repositoryMock.Verify(r => r.ListAsNoTrackingAsync<Reminder>(
                It.IsAny<Expression<Func<Reminder, bool>>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<string[]>()), Times.Once, "Should call ListAsNoTrackingAsync with a filtering predicate");

            // 3. Verify the correct data is returned (even if we mocked the behavior, the service logic must handle it)
            // The service currently filters manually in memory.
            // If we successfully filter in DB, the service should still work correctly.
            result.UpcomingEvents.Should().Contain(e => e.Title == "Active Reminder");
            result.UpcomingEvents.Should().Contain(e => e.Title == "Completed Recurring Reminder");
            result.UpcomingEvents.Should().NotContain(e => e.Title == "Completed One-Time Reminder");
        }
    }
}
