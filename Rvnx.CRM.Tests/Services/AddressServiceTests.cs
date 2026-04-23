using Moq;
using Rvnx.CRM.Core.DTOs.Contact;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Infrastructure.Services;

namespace Rvnx.CRM.Tests.Services;

public class AddressServiceTests
{
    private readonly Mock<IRepository> _repositoryMock;
    private readonly AddressService _service;

    public AddressServiceTests()
    {
        _repositoryMock = new Mock<IRepository>();
        _service = new AddressService(_repositoryMock.Object);
    }

    [Fact]
    public async Task CreateAsyncWhenContactNotFoundReturnsFailure()
    {
        // Arrange
        Guid contactId = Guid.NewGuid();
        AddressFormDto dto = new() { EntityId = contactId, Line1 = "123 Main St", AddressType = "Home" };

        _repositoryMock.Setup(r => r.CountAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<Contact, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        // Act
        OperationResult result = await _service.CreateAsync(dto);

        // Assert
        Assert.False(result.Success);
        Assert.True(result.IsNotFound);
        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<Address>(), It.IsAny<CancellationToken>()), Times.Never);
        _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsyncWhenAddressNotFoundReturnsFailure()
    {
        // Arrange
        Guid addressId = Guid.NewGuid();
        AddressFormDto dto = new() { EntityId = Guid.NewGuid(), Line1 = "456 Elm St", AddressType = "Work" };

        _repositoryMock.Setup(r => r.GetByIdAsync<Address>(addressId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Address?)null);

        // Act
        OperationResult result = await _service.UpdateAsync(addressId, dto);

        // Assert
        Assert.False(result.Success);
        Assert.True(result.IsNotFound);
        _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Address>(), It.IsAny<CancellationToken>()), Times.Never);
        _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetFormAsyncWhenAddressExistsButContactInvalidReturnsNull()
    {
        // Arrange
        Guid addressId = Guid.NewGuid();
        Guid contactId = Guid.NewGuid();
        Address address = new() { Id = addressId, ContactId = contactId, Line1 = "789 Oak Ave", AddressType = "Home" };

        _repositoryMock.Setup(r => r.GetByIdAsync<Address>(addressId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(address);

        _repositoryMock.Setup(r => r.CountAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<Contact, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(0); // IsValidContactAsync

        // Act
        AddressFormDto? result = await _service.GetFormAsync(addressId);

        // Assert
        Assert.Null(result);
    }
}
