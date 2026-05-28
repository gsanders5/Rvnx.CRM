using Moq;
using Rvnx.CRM.Core.DTOs.Contact;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Core.Services;
using System.Linq.Expressions;

namespace Rvnx.CRM.Tests.Services;

public class RelationshipSuggestionServiceTests
{
    private readonly Mock<IRepository> _repositoryMock;
    private readonly RelationshipSuggestionService _service;

    public RelationshipSuggestionServiceTests()
    {
        _repositoryMock = new Mock<IRepository>();
        _service = new RelationshipSuggestionService(_repositoryMock.Object);
    }

    [Fact]
    public async Task RelationshipDuplicateExistsAsyncWithBidirectionalMatchReturnsTrue()
    {
        Guid entityIdA = Guid.NewGuid();
        Guid entityIdB = Guid.NewGuid();
        Guid typeId = Guid.NewGuid();

        _repositoryMock.Setup(r => r.CountAsync<Relationship>(
                It.IsAny<Expression<Func<Relationship, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        bool result = await _service.RelationshipDuplicateExistsAsync(entityIdB, entityIdA, typeId, excludeId: null);

        Assert.True(result);
    }

    [Fact]
    public async Task RelationshipDuplicateExistsAsyncWithExcludeIdMatchingReturnsFalse()
    {
        Guid entityIdA = Guid.NewGuid();
        Guid entityIdB = Guid.NewGuid();
        Guid typeId = Guid.NewGuid();
        Guid existingRelId = Guid.NewGuid();

        _repositoryMock.Setup(r => r.CountAsync<Relationship>(
                It.IsAny<Expression<Func<Relationship, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        bool result = await _service.RelationshipDuplicateExistsAsync(entityIdA, entityIdB, typeId, excludeId: existingRelId);

        Assert.False(result);
    }

    [Fact]
    public async Task GetSuggestedRelationshipsAsyncWithNonTransitiveNonFamilyTypeReturnsEmpty()
    {
        Guid nonTransitiveTypeId = RelationshipTypeIds.Spouse;
        Guid entityId = Guid.NewGuid();
        Guid relatedEntityId = Guid.NewGuid();

        List<SuggestedRelationshipDto> result =
            await _service.GetSuggestedRelationshipsAsync(entityId, relatedEntityId, nonTransitiveTypeId, false, null);

        Assert.Empty(result);

        _repositoryMock.Verify(
            r => r.GetByIdAsync<Contact>(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _repositoryMock.Verify(
            r => r.ListAsNoTrackingAsync<Relationship>(
                It.IsAny<Expression<Func<Relationship, bool>>>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "Test names follow a standard convention")]
    public async Task GetSuggestedRelationshipsAsync_WithTransitiveType_FindsAndSuggestsUnlinkedContacts()
    {
        // Arrange
        Guid aliceId = Guid.NewGuid();
        Guid bobId = Guid.NewGuid();
        Guid charlieId = Guid.NewGuid();
        Guid daveId = Guid.NewGuid();

        var alice = new Contact { Id = aliceId, FirstName = "Alice", LastName = "Smith" };
        var bob = new Contact { Id = bobId, FirstName = "Bob", LastName = "Smith" };
        var charlie = new Contact { Id = charlieId, FirstName = "Charlie", LastName = "Jones" };
        var dave = new Contact { Id = daveId, FirstName = "Dave", LastName = "Jones" };

        var rels = new List<Relationship>
        {
            new Relationship { ContactId = aliceId, RelatedContactId = bobId, RelationshipTypeId = RelationshipTypeIds.Sibling },
            new Relationship { ContactId = charlieId, RelatedContactId = daveId, RelationshipTypeId = RelationshipTypeIds.Sibling }
        };
        var contacts = new List<Contact> { alice, bob, charlie, dave };

        _repositoryMock.Setup(r => r.GetByIdAsync<Contact>(aliceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(alice);

        _repositoryMock.Setup(r => r.GetByIdAsync<Contact>(charlieId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(charlie);

        _repositoryMock.Setup(r => r.ListAsNoTrackingAsync<Relationship>(
                It.IsAny<Expression<Func<Relationship, bool>>>(), It.IsAny<CancellationToken>(), It.IsAny<string[]>()))
            .ReturnsAsync((Expression<Func<Relationship, bool>> expr, CancellationToken ct, string[] includes) =>
                rels.AsQueryable().Where(expr).ToList());

        _repositoryMock.Setup(r => r.ListAsNoTrackingAsync<Contact>(
                It.IsAny<Expression<Func<Contact, bool>>>(), It.IsAny<CancellationToken>(), It.IsAny<string[]>()))
            .ReturnsAsync((Expression<Func<Contact, bool>> expr, CancellationToken ct, string[] includes) =>
                contacts.AsQueryable().Where(expr).ToList());

        // Act
        var result = await _service.GetSuggestedRelationshipsAsync(aliceId, charlieId, RelationshipTypeIds.Sibling, false, null);

        // Assert
        Assert.NotEmpty(result);
        Assert.Contains(result, r => r.SourceName == "Bob Smith" && r.TargetName == "Charlie Jones" && r.RelationshipName == "Sibling");
        Assert.Contains(result, r => r.SourceName == "Alice Smith" && r.TargetName == "Dave Jones" && r.RelationshipName == "Sibling");
    }

    [Fact]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "Test names follow a standard convention")]
    public async Task GetSuggestedRelationshipsAsync_WithFamilyAdultChildType_FindsAndSuggestsChildSiblings()
    {
        // Arrange
        Guid parentId = Guid.NewGuid();
        Guid childAId = Guid.NewGuid();
        Guid childBId = Guid.NewGuid();

        var parent = new Contact { Id = parentId, FirstName = "Mom", LastName = "Smith" };
        var childA = new Contact { Id = childAId, FirstName = "Alice", LastName = "Smith" };
        var childB = new Contact { Id = childBId, FirstName = "Bob", LastName = "Smith" };

        var rels = new List<Relationship>
        {
            new Relationship { ContactId = childAId, RelatedContactId = childBId, RelationshipTypeId = RelationshipTypeIds.Sibling },
        };
        var contacts = new List<Contact> { parent, childA, childB };

        _repositoryMock.Setup(r => r.GetByIdAsync<Contact>(parentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(parent);

        _repositoryMock.Setup(r => r.GetByIdAsync<Contact>(childAId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(childA);

        _repositoryMock.Setup(r => r.ListAsNoTrackingAsync<Relationship>(
                It.IsAny<Expression<Func<Relationship, bool>>>(), It.IsAny<CancellationToken>(), It.IsAny<string[]>()))
            .ReturnsAsync((Expression<Func<Relationship, bool>> expr, CancellationToken ct, string[] includes) =>
                rels.AsQueryable().Where(expr).ToList());

        _repositoryMock.Setup(r => r.ListAsNoTrackingAsync<Contact>(
                It.IsAny<Expression<Func<Contact, bool>>>(), It.IsAny<CancellationToken>(), It.IsAny<string[]>()))
            .ReturnsAsync((Expression<Func<Contact, bool>> expr, CancellationToken ct, string[] includes) =>
                contacts.AsQueryable().Where(expr).ToList());

        // Act
        var result = await _service.GetSuggestedRelationshipsAsync(parentId, childAId, RelationshipTypeIds.Parent, false, null);

        // Assert
        Assert.NotEmpty(result);
        Assert.Contains(result, r => r.SourceName == "Mom Smith" && r.TargetName == "Bob Smith" && r.RelationshipName == "Parent");
    }

    [Fact]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "Test names follow a standard convention")]
    public async Task GetSuggestedRelationshipsAsync_WhenContactIsNull_ReturnsEmptyList()
    {
        // Arrange
        Guid contactId = Guid.NewGuid();
        _repositoryMock.Setup(r => r.GetByIdAsync<Contact>(contactId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Contact?)null);

        // Act
        var result = await _service.GetSuggestedRelationshipsAsync(contactId, null, RelationshipTypeIds.Sibling, false, null);

        // Assert
        Assert.Empty(result);
    }
}
