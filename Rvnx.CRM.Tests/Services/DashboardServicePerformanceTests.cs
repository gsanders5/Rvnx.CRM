using Microsoft.Extensions.Logging;
using Moq;
using Rvnx.CRM.Core.DTOs.Dashboard;
using Rvnx.CRM.Core.Extensions;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models.Base;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Core.Models.Dates;
using Rvnx.CRM.Core.Services;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;

namespace Rvnx.CRM.Tests.Services;

public class DashboardServicePerformanceTests
{
    [Fact]
    [SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "Test names can contain underscores for readability.")]
    public async Task GetDashboardDataAsync_PerformanceBenchmark()
    {
        // Arrange
        var repositoryMock = new Mock<IRepository>();
        var loggerMock = new Mock<ILogger<DashboardService>>();
        var service = new DashboardService(repositoryMock.Object, loggerMock.Object);

        int numContacts = 10000;
        var contacts = Enumerable.Range(0, numContacts).Select(i => new Contact
        {
            Id = Guid.NewGuid(),
            FirstName = $"Contact{i}",
            LastName = "Test",
            IsHidden = false,
            IsPartial = false,
            CreatedDate = DateTime.UtcNow.AddDays(-10),
            LastChangedDate = DateTime.UtcNow.AddDays(-1)
        }).ToList();

        repositoryMock.Setup(r => r.ListAsNoTrackingAsync<Contact>(It.IsAny<Expression<Func<Contact, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(contacts);

        // the extension method delegates to ListProjectedAsync
        repositoryMock.Setup(r => r.ListProjectedAsync<Attachment, (Guid, Guid)>(
                It.IsAny<Expression<Func<Attachment, bool>>>(),
                It.IsAny<Expression<Func<Attachment, (Guid, Guid)>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<(Guid, Guid)>());

        repositoryMock.Setup(r => r.ListProjectedAsync<Relationship, (Guid EntityId, Guid RelatedEntityId)>(
                It.IsAny<Expression<Func<Relationship, bool>>>(),
                It.IsAny<Expression<Func<Relationship, (Guid EntityId, Guid RelatedEntityId)>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<(Guid, Guid)>());

        repositoryMock.Setup(r => r.ListAsNoTrackingAsync<SignificantDate>(It.IsAny<Expression<Func<SignificantDate, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SignificantDate>());

        // Simulating the CountAsync for birthdays that we are optimizing
        repositoryMock.Setup(r => r.CountAsync<SignificantDate>(It.IsAny<Expression<Func<SignificantDate, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(100)
            .Callback<Expression<Func<SignificantDate, bool>>, CancellationToken>((expr, ct) => {
                var comp = expr.Compile();
                var sw = Stopwatch.StartNew();
                // Simulating compilation and evaluation of the large expression tree (contains 10000 ids)
                for(int i = 0; i < 100; i++)
                {
                    comp(new SignificantDate { ContactId = contacts[0].Id, Title = "Birthday", EventDate = new DateOnly(1990, 1, 1) });
                }
                sw.Stop();
                Console.WriteLine($"Evaluating predicate took: {sw.ElapsedMilliseconds}ms");
            });

        repositoryMock.Setup(r => r.CountAsync<Contact>(It.IsAny<Expression<Func<Contact, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        // Act
        var sw = Stopwatch.StartNew();
        var result = await service.GetDashboardDataAsync();
        sw.Stop();

        // Assert
        Console.WriteLine($"GetDashboardDataAsync took {sw.ElapsedMilliseconds} ms for {numContacts} contacts.");
        Assert.NotNull(result);
    }
}
