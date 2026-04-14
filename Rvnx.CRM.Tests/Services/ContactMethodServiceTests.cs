using Moq;
using Rvnx.CRM.Core.Constants;
using Rvnx.CRM.Core.DTOs.Contact;
using Rvnx.CRM.Core.Enumerations;
using Rvnx.CRM.Core.Exceptions;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Infrastructure.Services;

namespace Rvnx.CRM.Tests.Services;

public class ContactMethodServiceTests
{
    private readonly Mock<IRepository> _repositoryMock;
    private readonly ContactMethodService _service;

    public ContactMethodServiceTests()
    {
        _repositoryMock = new Mock<IRepository>();
        _service = new ContactMethodService(_repositoryMock.Object);
    }

    [Fact]
    public async Task UpdateAsyncThrowsEntityConcurrencyExceptionWhenContactMethodExistsRethrows()
    {
        Guid contactMethodId = Guid.NewGuid();
        Guid contactId = Guid.NewGuid();
        ContactMethod existingContactMethod = new()
        { Id = contactMethodId, ContactId = contactId };
        ContactMethodFormDto dto = new()
        { Id = contactMethodId, EntityId = contactId, EntityType = EntityTypes.Person };

        _repositoryMock.Setup(r => r.GetByIdAsync<ContactMethod>(contactMethodId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingContactMethod);

        _repositoryMock.Setup(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Contact, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _repositoryMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new EntityConcurrencyException("Concurrency conflict"));

        _repositoryMock.Setup(r => r.ExistsAsync<ContactMethod>(contactMethodId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        await Assert.ThrowsAsync<EntityConcurrencyException>(() => _service.UpdateAsync(contactMethodId, dto));
    }

    [Fact]
    public async Task UpdateAsyncReturnsOkWhenValidAndUpdatesMethod()
    {
        Guid contactMethodId = Guid.NewGuid();
        Guid contactId = Guid.NewGuid();
        ContactMethod existingContactMethod = new()
        { Id = contactMethodId, ContactId = contactId, Type = ContactMethodType.Email, Value = "old@example.com" };
        ContactMethodFormDto dto = new()
        { Id = contactMethodId, EntityId = contactId, EntityType = EntityTypes.Person, Type = ContactMethodType.Email, Value = "new@example.com" };

        _repositoryMock.Setup(r => r.GetByIdAsync<ContactMethod>(contactMethodId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingContactMethod);

        _repositoryMock.Setup(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Contact, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        OperationResult result = await _service.UpdateAsync(contactMethodId, dto);

        Assert.True(result.Success);
        Assert.Equal(contactId, result.RedirectId);
        _repositoryMock.Verify(r => r.UpdateAsync(It.Is<ContactMethod>(cm => cm.Value == "new@example.com"), It.IsAny<CancellationToken>()), Times.Once);
        _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsyncThrowsEntityConcurrencyExceptionWhenContactMethodDoesNotExistReturnsFailure()
    {
        Guid contactMethodId = Guid.NewGuid();
        Guid contactId = Guid.NewGuid();
        ContactMethod existingContactMethod = new()
        { Id = contactMethodId, ContactId = contactId };
        ContactMethodFormDto dto = new()
        { Id = contactMethodId, EntityId = contactId, EntityType = EntityTypes.Person };

        _repositoryMock.Setup(r => r.GetByIdAsync<ContactMethod>(contactMethodId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingContactMethod);

        _repositoryMock.Setup(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Contact, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _repositoryMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new EntityConcurrencyException("Concurrency conflict"));

        _repositoryMock.Setup(r => r.ExistsAsync<ContactMethod>(contactMethodId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        OperationResult result = await _service.UpdateAsync(contactMethodId, dto);

        Assert.False(result.Success);
        Assert.Equal("Contact method not found.", result.ErrorMessage);
    }

    [Fact]
    public async Task DeleteAsyncWhenFoundReturnsOkAndDeletes()
    {
        Guid contactMethodId = Guid.NewGuid();
        Guid contactId = Guid.NewGuid();
        List<Guid?> contactIds = [contactId];

        _repositoryMock.Setup(r => r.ListProjectedAsync<ContactMethod, Guid?>(
            It.IsAny<System.Linq.Expressions.Expression<Func<ContactMethod, bool>>>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<ContactMethod, Guid?>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(contactIds);

        OperationResult result = await _service.DeleteAsync(contactMethodId);

        Assert.True(result.Success);
        Assert.Equal(contactId, result.RedirectId);
        Assert.Equal(EntityTypes.Person, result.RedirectType);

        _repositoryMock.Verify(r => r.DeleteAsync(It.IsAny<System.Linq.Expressions.Expression<Func<ContactMethod, bool>>>(), It.IsAny<CancellationToken>()), Times.Once);
        _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsyncWhenNotFoundReturnsFailureAndDoesNotDelete()
    {
        Guid contactMethodId = Guid.NewGuid();

        _repositoryMock.Setup(r => r.ListProjectedAsync<ContactMethod, Guid?>(
            It.IsAny<System.Linq.Expressions.Expression<Func<ContactMethod, bool>>>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<ContactMethod, Guid?>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        OperationResult result = await _service.DeleteAsync(contactMethodId);

        Assert.False(result.Success);
        Assert.Equal("Contact method not found.", result.ErrorMessage);

        _repositoryMock.Verify(r => r.DeleteAsync(It.IsAny<System.Linq.Expressions.Expression<Func<ContactMethod, bool>>>(), It.IsAny<CancellationToken>()), Times.Never);
        _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetFormAsyncWhenContactMethodNotFoundReturnsNull()
    {
        Guid contactMethodId = Guid.NewGuid();

        _repositoryMock.Setup(r => r.GetByIdAsync<ContactMethod>(contactMethodId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ContactMethod?)null);

        ContactMethodFormDto? result = await _service.GetFormAsync(contactMethodId);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetFormAsyncWhenContactIsInvalidReturnsNull()
    {
        Guid contactMethodId = Guid.NewGuid();
        Guid contactId = Guid.NewGuid();
        ContactMethod contactMethod = new() { Id = contactMethodId, ContactId = contactId };

        _repositoryMock.Setup(r => r.GetByIdAsync<ContactMethod>(contactMethodId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(contactMethod);
        _repositoryMock.Setup(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Contact, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        ContactMethodFormDto? result = await _service.GetFormAsync(contactMethodId);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetFormAsyncWhenContactIsValidReturnsDto()
    {
        Guid contactMethodId = Guid.NewGuid();
        Guid contactId = Guid.NewGuid();
        ContactMethod contactMethod = new()
        {
            Id = contactMethodId,
            ContactId = contactId,
            Type = ContactMethodType.Email,
            Value = "test@example.com",
            Label = ContactMethodLabels.Primary
        };

        _repositoryMock.Setup(r => r.GetByIdAsync<ContactMethod>(contactMethodId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(contactMethod);
        _repositoryMock.Setup(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Contact, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        ContactMethodFormDto? result = await _service.GetFormAsync(contactMethodId);

        Assert.NotNull(result);
        Assert.Equal(contactMethodId, result.Id);
        Assert.Equal(contactId, result.EntityId);
        Assert.Equal(EntityTypes.Person, result.EntityType);
        Assert.Equal(ContactMethodType.Email, result.Type);
        Assert.Equal("test@example.com", result.Value);
        Assert.Equal(ContactMethodLabels.Primary, result.Label);
    }

    [Fact]
    public async Task CreateAsyncReturnsOkWhenValidContact()
    {
        Guid contactId = Guid.NewGuid();
        ContactMethodFormDto dto = new()
        {
            EntityId = contactId,
            EntityType = EntityTypes.Person,
            Type = ContactMethodType.Email,
            Value = "new@example.com"
        };

        _repositoryMock.Setup(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Contact, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        OperationResult result = await _service.CreateAsync(dto);

        Assert.True(result.Success);
        Assert.Equal(contactId, result.RedirectId);
        _repositoryMock.Verify(r => r.AddAsync(It.Is<ContactMethod>(cm => cm.Value == "new@example.com"), It.IsAny<CancellationToken>()), Times.Once);
        _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsyncReturnsFailureWhenInvalidContact()
    {
        Guid contactId = Guid.NewGuid();
        ContactMethodFormDto dto = new()
        {
            EntityId = contactId,
            EntityType = EntityTypes.Person,
            Type = ContactMethodType.Email,
            Value = "new@example.com"
        };

        _repositoryMock.Setup(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Contact, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        OperationResult result = await _service.CreateAsync(dto);

        Assert.False(result.Success);
        Assert.Equal("Contact not found.", result.ErrorMessage);
        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<ContactMethod>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}