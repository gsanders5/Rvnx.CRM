using Moq;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Core.Services;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Threading.Tasks;
using System.Threading;
using System;
using Xunit;


namespace Rvnx.CRM.Tests.Services;

public class ContactLookupServiceTests
{
    private readonly Mock<IRepository> _repositoryMock;
    private readonly ContactLookupService _service;

    public ContactLookupServiceTests()
    {
        _repositoryMock = new Mock<IRepository>();
        _service = new ContactLookupService(_repositoryMock.Object);
    }

    [Fact]
    public async Task ExistsAsyncPersonCallsRepositoryForContact()
    {
        Guid id = Guid.NewGuid();
        _repositoryMock.Setup(r => r.ExistsAsync<Contact>(id, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        bool result = await _service.ExistsAsync(id);

        Assert.True(result);
        _repositoryMock.Verify(r => r.ExistsAsync<Contact>(id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetContactNameAsyncPersonReturnsFullName()
    {
        Guid id = Guid.NewGuid();
        _repositoryMock.Setup(r => r.ListProjectedAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<Contact, bool>>>(),
                It.IsAny<System.Linq.Expressions.Expression<Func<Contact, string>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(["John Doe"]);

        string result = await _service.GetContactNameAsync(id);

        Assert.Equal("John Doe", result);
    }

    [Fact]
    public async Task GetContactNameAsyncPersonWhenNotFoundReturnsUnknownPerson()
    {
        Guid id = Guid.NewGuid();
        _repositoryMock.Setup(r => r.ListProjectedAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<Contact, bool>>>(),
                It.IsAny<System.Linq.Expressions.Expression<Func<Contact, string>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        string result = await _service.GetContactNameAsync(id);

        Assert.Equal("Unknown Person", result);
    }

    [Fact]
    public async Task IsPartialAsyncPersonPartialReturnsTrue()
    {
        Guid id = Guid.NewGuid();
        _repositoryMock.Setup(r => r.ListProjectedAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<Contact, bool>>>(),
                It.IsAny<System.Linq.Expressions.Expression<Func<Contact, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([true]);

        bool result = await _service.IsPartialAsync(id);

        Assert.True(result);
    }

    [Fact]
    public async Task IsPartialAsyncPersonWhenNotFoundReturnsFalse()
    {
        Guid id = Guid.NewGuid();
        _repositoryMock.Setup(r => r.ListProjectedAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<Contact, bool>>>(),
                It.IsAny<System.Linq.Expressions.Expression<Func<Contact, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        bool result = await _service.IsPartialAsync(id);

        Assert.False(result);
    }

    [Fact]
    public async Task IsPartialAsyncPersonNotPartialReturnsFalse()
    {
        Guid id = Guid.NewGuid();
        _repositoryMock.Setup(r => r.ListProjectedAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<Contact, bool>>>(),
                It.IsAny<System.Linq.Expressions.Expression<Func<Contact, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([false]);

        bool result = await _service.IsPartialAsync(id);

        Assert.False(result);
    }


    [Fact]
    [SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "Test names follow a standard convention")]
    public async Task GetPartialContactIdsAsync_WhenIdsEmpty_ReturnsEmptyHashSetAndDoesNotCallRepository()
    {
        // Act
        HashSet<Guid> result = await _service.GetPartialContactIdsAsync(new List<Guid>());

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
        _repositoryMock.Verify(r => r.ListProjectedAsync(
            It.IsAny<Expression<Func<Contact, bool>>>(),
            It.IsAny<Expression<Func<Contact, Guid>>>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "Test names follow a standard convention")]
    public async Task GetPartialContactIdsAsync_WhenIdsProvided_ReturnsHashSetOfPartialIds()
    {
        // Arrange
        Guid id1 = Guid.NewGuid();
        Guid id2 = Guid.NewGuid();
        List<Guid> ids = new() { id1, id2 };

        _repositoryMock.Setup(r => r.ListProjectedAsync(
            It.IsAny<Expression<Func<Contact, bool>>>(),
            It.IsAny<Expression<Func<Contact, Guid>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Guid> { id1 });

        // Act
        HashSet<Guid> result = await _service.GetPartialContactIdsAsync(ids);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Contains(id1, result);
    }
}
