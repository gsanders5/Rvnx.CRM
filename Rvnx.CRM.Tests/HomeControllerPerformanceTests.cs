using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Core.Models.Dates;
using Rvnx.CRM.Web.Controllers;
using System.Linq.Expressions;

namespace Rvnx.CRM.Tests;

public class HomeControllerPerformanceTests
{
    private readonly Mock<IRepository> _repositoryMock;
    private readonly Mock<ILogger<HomeController>> _loggerMock;
    private readonly HomeController _controller;

    public HomeControllerPerformanceTests()
    {
        _repositoryMock = new Mock<IRepository>();
        _loggerMock = new Mock<ILogger<HomeController>>();
        _controller = new HomeController(_loggerMock.Object, _repositoryMock.Object);
    }

    [Fact]
    public async Task Index_ShouldCallListAsNoTrackingAsync_AfterOptimization()
    {
        // Arrange
        // Contact uses predicate overload
        _repositoryMock.Setup(r => r.ListAsNoTrackingAsync<Contact>(
                It.IsAny<Expression<Func<Contact, bool>>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<string[]>()))
            .ReturnsAsync(new List<Contact>());

        // Reminder uses pagination overload (no predicate, just skip/take/cancellation)
        _repositoryMock.Setup(r => r.ListAsNoTrackingAsync<Reminder>(
                It.IsAny<int?>(),
                It.IsAny<int?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Reminder>());

        _repositoryMock.Setup(r => r.ListAsNoTrackingAsync<SignificantDate>(
                It.IsAny<Expression<Func<SignificantDate, bool>>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<string[]>()))
            .ReturnsAsync(new List<SignificantDate>());

        _repositoryMock.Setup(r => r.ListAsNoTrackingAsync<Relationship>(
                It.IsAny<Expression<Func<Relationship, bool>>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<string[]>()))
            .ReturnsAsync(new List<Relationship>());

        // Act
        IActionResult result = await _controller.Index();

        // Assert
        _repositoryMock.Verify(r => r.ListAsNoTrackingAsync<Contact>(
            It.IsAny<Expression<Func<Contact, bool>>>(),
            It.IsAny<CancellationToken>(),
            It.IsAny<string[]>()), Times.Once);

        _repositoryMock.Verify(r => r.ListAsNoTrackingAsync<Reminder>(
            It.IsAny<int?>(),
            It.IsAny<int?>(),
            It.IsAny<CancellationToken>()), Times.Once);

        _repositoryMock.Verify(r => r.ListAsNoTrackingAsync<SignificantDate>(
            It.IsAny<Expression<Func<SignificantDate, bool>>>(),
            It.IsAny<CancellationToken>(),
            It.IsAny<string[]>()), Times.Once);

        _repositoryMock.Verify(r => r.ListAsNoTrackingAsync<Relationship>(
            It.IsAny<Expression<Func<Relationship, bool>>>(),
            It.IsAny<CancellationToken>(),
            It.IsAny<string[]>()), Times.Once);

        result.Should().BeOfType<ViewResult>();
    }
}
