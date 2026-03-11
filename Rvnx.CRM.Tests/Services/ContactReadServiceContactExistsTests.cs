using Moq;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Core.Services;

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
        // Arrange
        var contactId = Guid.NewGuid();
        var contact = new Contact { Id = contactId, IsPartial = false };

        _repositoryMock.Setup(r => r.GetByIdAsync<Contact>(contactId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(contact);

        // Act
        var result = await _service.ContactExistsAsync(contactId);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task ContactExistsAsyncWhenContactDoesNotExistReturnsFalse()
    {
        // Arrange
        var contactId = Guid.NewGuid();

        _repositoryMock.Setup(r => r.GetByIdAsync<Contact>(contactId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Contact?)null);

        // Act
        var result = await _service.ContactExistsAsync(contactId);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ContactExistsAsyncWhenContactIsPartialReturnsFalse()
    {
        // Arrange
        var contactId = Guid.NewGuid();
        var contact = new Contact { Id = contactId, IsPartial = true };

        _repositoryMock.Setup(r => r.GetByIdAsync<Contact>(contactId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(contact);

        // Act
        var result = await _service.ContactExistsAsync(contactId);

        // Assert
        Assert.False(result);
    }
}
