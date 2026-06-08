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
    public async Task GetSuggestedRelationshipsAsync_TransitiveRelationship_ReturnsValidSuggestions()
    {
        // Arrange
        Guid siblingTypeId = RelationshipTypeIds.Sibling;
        Guid contactAId = Guid.NewGuid(); // Alice
        Guid contactBId = Guid.NewGuid(); // Bob
        Guid contactCId = Guid.NewGuid(); // Charlie

        List<Contact> contacts =
        [
            new() { Id = contactAId, FirstName = "Alice", LastName = "Smith" },
            new() { Id = contactBId, FirstName = "Bob", LastName = "Smith" },
            new() { Id = contactCId, FirstName = "Charlie", LastName = "Smith" }
        ];

        // Existing relationship: Alice is Sibling of Bob
        List<Relationship> relationships =
        [
            new() { Id = Guid.NewGuid(), ContactId = contactAId, RelatedContactId = contactBId, RelationshipTypeId = siblingTypeId }
        ];

        _repositoryMock.Setup(r => r.GetByIdAsync<Contact>(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, CancellationToken ct) => contacts.FirstOrDefault(c => c.Id == id));

        _repositoryMock.Setup(r => r.ListAsNoTrackingAsync<Contact>(
                It.IsAny<Expression<Func<Contact, bool>>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<string[]>()))
            .ReturnsAsync((Expression<Func<Contact, bool>> expr, CancellationToken ct, string[] includes) =>
                contacts.AsQueryable().Where(expr).ToList());

        _repositoryMock.Setup(r => r.ListAsNoTrackingAsync<Relationship>(
                It.IsAny<Expression<Func<Relationship, bool>>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<string[]>()))
            .ReturnsAsync((Expression<Func<Relationship, bool>> expr, CancellationToken ct, string[] includes) =>
                relationships.AsQueryable().Where(expr).ToList());

        // Act
        // Suggest relationships for Charlie (contactC) targeting Alice (contactA)
        var result = await _service.GetSuggestedRelationshipsAsync(contactCId, contactAId, siblingTypeId, false, "Alice Smith");

        // Assert
        Assert.Single(result);
        Assert.Equal("Charlie Smith", result[0].SourceName);
        Assert.Equal("Bob Smith", result[0].TargetName);
        Assert.Equal("Sibling", result[0].RelationshipName);
    }
}
