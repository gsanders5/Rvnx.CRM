using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Rvnx.CRM.Core.DTOs.Dashboard;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Web.Controllers;

namespace Rvnx.CRM.Tests.Controllers;

public class HomeControllerPerformanceTests : IDisposable
{
    private readonly Mock<IDashboardService> _dashboardServiceMock;
    private readonly HomeController _controller;

    public HomeControllerPerformanceTests()
    {
        _dashboardServiceMock = new Mock<IDashboardService>();
        _controller = new HomeController(_dashboardServiceMock.Object);
    }

    public void Dispose()
    {
        _controller?.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task IndexShouldDelegateToDashboardService()
    {
        // Arrange
        DashboardDto dashboardData = new()
        {
            UpcomingEvents =
            [
                new() { Title = "Test Event", Type = "Reminder", TimeUntil = "Tomorrow" }
            ],
            GraphNodes =
            [
                new() { Id = "1", Name = "Test Contact", Group = 1 }
            ],
            GraphLinks =
            [
                new() { Source = "1", Target = "2", Type = "Relationship" }
            ]
        };

        _dashboardServiceMock
            .Setup(s => s.GetDashboardDataAsync())
            .ReturnsAsync(dashboardData);

        // Act
        IActionResult result = await _controller.Index();

        // Assert
        _dashboardServiceMock.Verify(s => s.GetDashboardDataAsync(), Times.Once);
        result.Should().BeOfType<ViewResult>();
    }
}
