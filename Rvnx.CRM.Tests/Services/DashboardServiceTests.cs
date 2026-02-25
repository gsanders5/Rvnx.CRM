using Microsoft.Extensions.Logging;
using Moq;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Core.Models.Dates;
using Rvnx.CRM.Core.Services;
using System.Linq.Expressions;

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
        public async Task GetDashboardDataAsyncFiltersRemindersAtDatabaseLevel()
        {
            // Arrange
            List<Contact> contacts =
            [
                new Contact { Id = Guid.NewGuid(), FirstName = "John", LastName = "Doe" }
            ];

            List<Reminder> reminders =
            [
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
            ];

            // Setup mocks for other calls
            _repositoryMock.Setup(r => r.ListAsNoTrackingAsync<Contact>(It.IsAny<Expression<Func<Contact, bool>>>(),
                    It.IsAny<CancellationToken>(), It.IsAny<string[]>()))
                .ReturnsAsync(contacts);

            _repositoryMock.Setup(r =>
                    r.ListAsNoTrackingAsync<Relationship>(It.IsAny<Expression<Func<Relationship, bool>>>(),
                        It.IsAny<CancellationToken>(), It.IsAny<string[]>()))
                .ReturnsAsync([]);

            _repositoryMock.Setup(r =>
                    r.ListAsNoTrackingAsync<SignificantDate>(It.IsAny<Expression<Func<SignificantDate, bool>>>(),
                        It.IsAny<CancellationToken>(), It.IsAny<string[]>()))
                .ReturnsAsync([]);


            // Setup for the EXPECTED new behavior (with predicate)
            _repositoryMock.Setup(r => r.ListAsNoTrackingAsync<Reminder>(It.IsAny<Expression<Func<Reminder, bool>>>(),
                    It.IsAny<CancellationToken>(), It.IsAny<string[]>()))
                .ReturnsAsync((Expression<Func<Reminder, bool>> predicate, CancellationToken token,
                    string[] includes) =>
                {
                    return reminders.AsQueryable().Where(predicate).ToList();
                });

            // Setup for the OLD behavior (parameterless / skip-take overload)
            _repositoryMock.Setup(r =>
                    r.ListAsNoTrackingAsync<Reminder>(It.IsAny<int?>(), It.IsAny<int?>(),
                        It.IsAny<CancellationToken>()))
                .ReturnsAsync(reminders);

            // Act
            Core.DTOs.Dashboard.DashboardDto result = await _dashboardService.GetDashboardDataAsync();

            // Assert
            // 1. Verify the parameterless overload is NOT called (this fails initially)
            _repositoryMock.Verify(r => r.ListAsNoTrackingAsync<Reminder>(
                    It.IsAny<int?>(),
                    It.IsAny<int?>(),
                    It.IsAny<CancellationToken>()), Times.Never,
                "Should NOT call parameterless ListAsNoTrackingAsync (fetching all records)");

            // 2. Verify the predicate overload IS called
            _repositoryMock.Verify(r => r.ListAsNoTrackingAsync<Reminder>(
                It.IsAny<Expression<Func<Reminder, bool>>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<string[]>()), Times.Once, "Should call ListAsNoTrackingAsync with a filtering predicate");

            // 3. Verify only incomplete reminders appear — completed reminders should never show
            //    on the dashboard, regardless of whether they are recurring.
            Assert.Contains(result.UpcomingEvents, e => e.Title == "Active Reminder");
            Assert.DoesNotContain(result.UpcomingEvents, e => e.Title == "Completed Recurring Reminder");
            Assert.DoesNotContain(result.UpcomingEvents, e => e.Title == "Completed One-Time Reminder");
        }
    }
}
