using Moq;
using Rvnx.CRM.Core.Constants;
using Rvnx.CRM.Core.DTOs.Contact;
using Rvnx.CRM.Core.Exceptions;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Infrastructure.Services;

namespace Rvnx.CRM.Tests.Services;

public class FactServiceTests
{
    private readonly Mock<IRepository> _repositoryMock;
    private readonly FactService _service;

    public FactServiceTests()
    {
        _repositoryMock = new Mock<IRepository>();
        _service = new FactService(_repositoryMock.Object);
    }

    [Fact]
    public async Task UpdateAsyncThrowsEntityConcurrencyExceptionWhenFactExistsRethrows()
    {
        Guid factId = Guid.NewGuid();
        Guid contactId = Guid.NewGuid();
        Fact existingFact = new()
        { Id = factId, ContactId = contactId };
        FactFormDto dto = new()
        { Id = factId, EntityId = contactId, EntityType = EntityTypes.Person };

        _repositoryMock.Setup(r => r.GetByIdAsync<Fact>(factId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingFact);

        _repositoryMock.Setup(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Contact, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _repositoryMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new EntityConcurrencyException("Concurrency conflict"));

        _repositoryMock.Setup(r => r.ExistsAsync<Fact>(factId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act & Assert
        await Assert.ThrowsAsync<EntityConcurrencyException>(() => _service.UpdateAsync(factId, dto));
    }

    [Fact]
    public async Task UpdateAsyncThrowsEntityConcurrencyExceptionWhenFactDoesNotExistReturnsFailure()
    {
        Guid factId = Guid.NewGuid();
        Guid contactId = Guid.NewGuid();
        Fact existingFact = new()
        { Id = factId, ContactId = contactId };
        FactFormDto dto = new()
        { Id = factId, EntityId = contactId, EntityType = EntityTypes.Person };

        _repositoryMock.Setup(r => r.GetByIdAsync<Fact>(factId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingFact);

        _repositoryMock.Setup(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Contact, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _repositoryMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new EntityConcurrencyException("Concurrency conflict"));

        _repositoryMock.Setup(r => r.ExistsAsync<Fact>(factId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        OperationResult result = await _service.UpdateAsync(factId, dto);

        Assert.False(result.Success);
        Assert.Equal("Fact not found.", result.ErrorMessage);
    }
}