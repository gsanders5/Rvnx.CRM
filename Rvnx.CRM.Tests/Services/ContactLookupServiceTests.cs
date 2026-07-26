using Moq;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Core.Services;

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
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "Test names follow a standard convention")]
    public async Task GetContactNameAsync_WhenLastNameIsNull_ReturnsTrimmedFirstName()
    {
        Guid id = Guid.NewGuid();

        System.Linq.Expressions.Expression<Func<Contact, string>>? capturedProjection = null;

        _repositoryMock.Setup(r => r.ListProjectedAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<Contact, bool>>>(),
                It.IsAny<System.Linq.Expressions.Expression<Func<Contact, string>>>(),
                It.IsAny<CancellationToken>()))
            .Callback<System.Linq.Expressions.Expression<Func<Contact, bool>>, System.Linq.Expressions.Expression<Func<Contact, string>>, CancellationToken>((filter, projection, ct) =>
            {
                capturedProjection = projection;
            })
            .ReturnsAsync(["TestName "]);

        string result = await _service.GetContactNameAsync(id);

        Assert.NotNull(capturedProjection);
        Func<Contact, string> compiledFunc = capturedProjection.Compile();
        Contact contact = new() { FirstName = "John", LastName = null };
        string projectedResult = compiledFunc(contact);

        Assert.Equal("John ", projectedResult);
        Assert.Equal("TestName", result);
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
    public async Task GetPartialContactIdsAsyncWithEmptyInputReturnsEmptySetAndDoesNotCallRepository()
    {
        // Act
        HashSet<Guid> result = await _service.GetPartialContactIdsAsync([]);

        // Assert
        Assert.Empty(result);
        _repositoryMock.Verify(
            r => r.ListProjectedAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<Contact, bool>>>(),
                It.IsAny<System.Linq.Expressions.Expression<Func<Contact, Guid>>>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetPartialContactIdsAsyncReturnsOnlyPartialIdsFromRepository()
    {
        // Arrange
        Guid partialId1 = Guid.NewGuid();
        Guid partialId2 = Guid.NewGuid();
        Guid nonPartialId = Guid.NewGuid();

        List<Guid> inputIds = [partialId1, partialId2, nonPartialId];

        // The mock repository will return the partial IDs
        _repositoryMock.Setup(r => r.ListProjectedAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<Contact, bool>>>(),
                It.IsAny<System.Linq.Expressions.Expression<Func<Contact, Guid>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([partialId1, partialId2]);

        // Act
        HashSet<Guid> result = await _service.GetPartialContactIdsAsync(inputIds);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(partialId1, result);
        Assert.Contains(partialId2, result);
        Assert.DoesNotContain(nonPartialId, result);

        // Verify repository was called exactly once
        _repositoryMock.Verify(
            r => r.ListProjectedAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<Contact, bool>>>(),
                It.IsAny<System.Linq.Expressions.Expression<Func<Contact, Guid>>>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

}
