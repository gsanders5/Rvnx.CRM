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
        System.Linq.Expressions.Expression<Func<Contact, string>>? capturedProjection = null;

        _repositoryMock.Setup(r => r.ListProjectedAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<Contact, bool>>>(),
                It.IsAny<System.Linq.Expressions.Expression<Func<Contact, string>>>(),
                It.IsAny<CancellationToken>()))
            .Callback<System.Linq.Expressions.Expression<Func<Contact, bool>>, System.Linq.Expressions.Expression<Func<Contact, string>>, CancellationToken>((predicate, projection, ct) =>
            {
                capturedProjection = projection;
            })
            .ReturnsAsync(["John Doe"]);

        string result = await _service.GetContactNameAsync(id);

        Assert.Equal("John Doe", result);
        Assert.NotNull(capturedProjection);

        Func<Contact, string> compiledProjection = capturedProjection!.Compile();
        Contact testContact = new() { FirstName = "John", LastName = "Doe" };
        Assert.Equal("John Doe", compiledProjection(testContact));
    }

    [Fact]
    public async Task GetContactNameAsyncPersonWithNoLastNameReturnsFirstNameOnlyTrimmed()
    {
        Guid id = Guid.NewGuid();
        System.Linq.Expressions.Expression<Func<Contact, string>>? capturedProjection = null;

        _repositoryMock.Setup(r => r.ListProjectedAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<Contact, bool>>>(),
                It.IsAny<System.Linq.Expressions.Expression<Func<Contact, string>>>(),
                It.IsAny<CancellationToken>()))
            .Callback<System.Linq.Expressions.Expression<Func<Contact, bool>>, System.Linq.Expressions.Expression<Func<Contact, string>>, CancellationToken>((predicate, projection, ct) =>
            {
                capturedProjection = projection;
            })
            .ReturnsAsync(["John "]); // The service trims it to "John"

        string result = await _service.GetContactNameAsync(id);

        Assert.Equal("John", result);
        Assert.NotNull(capturedProjection);

        Func<Contact, string> compiledProjection = capturedProjection!.Compile();
        Contact testContact = new() { FirstName = "John", LastName = null };
        Assert.Equal("John ", compiledProjection(testContact)); // The projection outputs "John " before the service trims it
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

}
