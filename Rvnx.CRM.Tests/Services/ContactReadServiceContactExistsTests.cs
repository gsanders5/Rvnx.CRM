using Moq;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Core.Services;
using System.Linq.Expressions;

namespace Rvnx.CRM.Tests.Services;

public class ContactReadServiceContactExistsTests
{
    private readonly Mock<IRepository> _repositoryMock;
    private readonly ContactReadService _service;

    public ContactReadServiceContactExistsTests()
    {
        _repositoryMock = new Mock<IRepository>();
        _service = new ContactReadService(_repositoryMock.Object);
    }

    [Fact]
    public async Task ContactExistsAsyncWhenContactExistsAndIsFullReturnsTrue()
    {
        Guid contactId = Guid.NewGuid();

        _repositoryMock.Setup(r => r.CountAsync<Contact>(It.IsAny<Expression<Func<Contact, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        bool result = await _service.ContactExistsAsync(contactId);

        Assert.True(result);
    }

    [Fact]
    public async Task ContactExistsAsyncWhenContactDoesNotExistReturnsFalse()
    {
        Guid contactId = Guid.NewGuid();

        _repositoryMock.Setup(r => r.CountAsync<Contact>(It.IsAny<Expression<Func<Contact, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        bool result = await _service.ContactExistsAsync(contactId);

        Assert.False(result);
    }

    [Fact]
    public async Task ContactExistsAsyncWhenContactIsPartialReturnsFalse()
    {
        Guid contactId = Guid.NewGuid();

        _repositoryMock.Setup(r => r.CountAsync<Contact>(It.IsAny<Expression<Func<Contact, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        bool result = await _service.ContactExistsAsync(contactId);

        Assert.False(result);
    }
}
