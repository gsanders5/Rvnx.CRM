using Moq;
using Rvnx.CRM.Core.Constants;
using Rvnx.CRM.Core.DTOs.Contact;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Core.Services;
using System.Linq.Expressions;

namespace Rvnx.CRM.Tests.Services;

public class ContactReadServiceGetContactDetailsTests
{
    private readonly Mock<IRepository> _repositoryMock;
    private readonly ContactReadService _service;

    public ContactReadServiceGetContactDetailsTests()
    {
        _repositoryMock = new Mock<IRepository>();
        _service = new ContactReadService(_repositoryMock.Object);
    }

    [Fact]
    public async Task GetContactDetailsAsyncReturnsContactDetailsWithRelationships()
    {
        // Arrange
        Guid contactId = Guid.NewGuid();
        Guid relatedId1 = Guid.NewGuid();
        Guid relatedId2 = Guid.NewGuid();

        Contact contact = new()
        { Id = contactId, FirstName = "Main", LastName = "User" };
        List<Contact> relatedContacts =
        [
            new Contact { Id = relatedId1, FirstName = "Child" },
            new Contact { Id = relatedId2, FirstName = "Parent" }
        ];

        List<Relationship> allRelationships =
        [
            new Relationship { Id = Guid.NewGuid(), EntityId = contactId, RelatedEntityId = relatedId1, EntityType = EntityTypes.Person, RelationshipTypeId = Guid.NewGuid() }, // outgoing
            new Relationship { Id = Guid.NewGuid(), EntityId = relatedId2, RelatedEntityId = contactId, EntityType = EntityTypes.Person, RelationshipTypeId = Guid.NewGuid() }  // incoming
        ];

        _repositoryMock.Setup(r => r.ListAsNoTrackingAsync<Contact>(
            It.IsAny<Expression<Func<Contact, bool>>>(),
            It.IsAny<CancellationToken>(),
            It.IsAny<string[]>()))
            .ReturnsAsync([contact]);

        _repositoryMock.Setup(r => r.ListAsNoTrackingAsync<Relationship>(
            It.IsAny<Expression<Func<Relationship, bool>>>(),
            It.IsAny<CancellationToken>(),
            It.IsAny<string[]>()))
            .ReturnsAsync(allRelationships);

        _repositoryMock.Setup(r => r.ListProjectedAsync(
            It.IsAny<Expression<Func<Contact, bool>>>(),
            It.IsAny<Expression<Func<Contact, Contact>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(relatedContacts);

        // Act
        ContactDetailDto? result = await _service.GetContactDetailsAsync(contactId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(contactId, result.Id);

        Assert.Single(result.Relationships); // Outgoing
        Assert.Equal(relatedId1, result.Relationships.First().RelatedEntityId);
        Assert.Equal("Child", result.Relationships.First().RelatedEntityName);

        Assert.Single(result.RelatedTo); // Incoming
        Assert.Equal(relatedId2, result.RelatedTo.First().EntityId);
        Assert.Equal("Parent", result.RelatedTo.First().EntityName);
    }

    [Fact]
    public async Task GetContactDetailsAsyncWhenContactDoesNotExistReturnsNull()
    {
        // Arrange
        Guid contactId = Guid.NewGuid();

        _repositoryMock.Setup(r => r.ListAsNoTrackingAsync<Contact>(
            It.IsAny<Expression<Func<Contact, bool>>>(),
            It.IsAny<CancellationToken>(),
            It.IsAny<string[]>()))
            .ReturnsAsync([]); // Returns empty list

        // Act
        ContactDetailDto? result = await _service.GetContactDetailsAsync(contactId);

        // Assert
        Assert.Null(result);
    }
}