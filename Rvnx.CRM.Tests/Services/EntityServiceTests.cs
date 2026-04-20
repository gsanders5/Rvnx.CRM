using Moq;
using Rvnx.CRM.Core.Enumerations;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models.Business;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Core.Services;

namespace Rvnx.CRM.Tests.Services;

public class EntityServiceTests
{
    private readonly Mock<IRepository> _repositoryMock;
    private readonly EntityService _service;

    public EntityServiceTests()
    {
        _repositoryMock = new Mock<IRepository>();
        _service = new EntityService(_repositoryMock.Object);
    }

    [Fact]
    public async Task ExistsAsyncPersonCallsRepositoryForContact()
    {
        Guid id = Guid.NewGuid();
        _repositoryMock.Setup(r => r.ExistsAsync<Contact>(id, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        bool result = await _service.ExistsAsync(EntityType.Person, id);

        Assert.True(result);
        _repositoryMock.Verify(r => r.ExistsAsync<Contact>(id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(EntityType.Company)]
    [InlineData(EntityType.Opportunity)]
    public async Task ExistsAsyncUnsupportedTypeReturnsFalse(EntityType entityType)
    {
        Guid id = Guid.NewGuid();

        bool result = await _service.ExistsAsync(entityType, id);

        Assert.False(result);
        Assert.Empty(_repositoryMock.Invocations);
    }

    [Fact]
    public async Task GetEntityNameAsyncPersonReturnsFullName()
    {
        Guid id = Guid.NewGuid();
        _repositoryMock.Setup(r => r.ListProjectedAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<Contact, bool>>>(),
                It.IsAny<System.Linq.Expressions.Expression<Func<Contact, string>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(["John Doe"]);

        string result = await _service.GetEntityNameAsync(EntityType.Person, id);

        Assert.Equal("John Doe", result);
    }

    [Fact]
    public async Task GetEntityNameAsyncPersonWhenNotFoundReturnsUnknownPerson()
    {
        Guid id = Guid.NewGuid();
        _repositoryMock.Setup(r => r.ListProjectedAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<Contact, bool>>>(),
                It.IsAny<System.Linq.Expressions.Expression<Func<Contact, string>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        string result = await _service.GetEntityNameAsync(EntityType.Person, id);

        Assert.Equal("Unknown Person", result);
    }

    [Fact]
    public async Task GetEntityNameAsyncCompanyReturnsCompanyName()
    {
        Guid id = Guid.NewGuid();
        _repositoryMock.Setup(r => r.ListProjectedAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<Employer, bool>>>(),
                It.IsAny<System.Linq.Expressions.Expression<Func<Employer, string>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(["Acme Corp"]);

        string result = await _service.GetEntityNameAsync(EntityType.Company, id);

        Assert.Equal("Acme Corp", result);
    }

    [Fact]
    public async Task GetEntityNameAsyncCompanyWhenNotFoundReturnsUnknownCompany()
    {
        Guid id = Guid.NewGuid();
        _repositoryMock.Setup(r => r.ListProjectedAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<Employer, bool>>>(),
                It.IsAny<System.Linq.Expressions.Expression<Func<Employer, string>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        string result = await _service.GetEntityNameAsync(EntityType.Company, id);

        Assert.Equal("Unknown Company", result);
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

        bool result = await _service.IsPartialAsync(EntityType.Person, id);

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

        bool result = await _service.IsPartialAsync(EntityType.Person, id);

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

        bool result = await _service.IsPartialAsync(EntityType.Person, id);

        Assert.False(result);
    }

    [Fact]
    public async Task IsPartialAsyncCompanyReturnsFalse()
    {
        Guid id = Guid.NewGuid();

        bool result = await _service.IsPartialAsync(EntityType.Company, id);

        Assert.False(result);
    }
}