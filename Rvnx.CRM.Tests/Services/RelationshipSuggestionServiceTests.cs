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
    public async Task GetSuggestedRelationshipsAsync_WithTransitiveType_ReturnsNetworkSuggestions()
    {
        // Arrange
        Guid entityId = Guid.NewGuid();
        Guid relatedEntityId = Guid.NewGuid();

        Guid otherEntityA = Guid.NewGuid();
        Guid otherEntityB = Guid.NewGuid();

        Guid typeId = RelationshipTypeIds.Sibling;

        List<Contact> mockContacts = new List<Contact>
        {
            new Contact { Id = entityId, FirstName = "Main", LastName = "User" },
            new Contact { Id = relatedEntityId, FirstName = "Related", LastName = "User" },
            new Contact { Id = otherEntityA, FirstName = "Other", LastName = "A" },
            new Contact { Id = otherEntityB, FirstName = "Other", LastName = "B" }
        };

        List<Relationship> mockRelationships = new List<Relationship>
        {
            new Relationship { Id = Guid.NewGuid(), ContactId = entityId, RelatedContactId = otherEntityA, RelationshipTypeId = typeId },
            new Relationship { Id = Guid.NewGuid(), ContactId = relatedEntityId, RelatedContactId = otherEntityB, RelationshipTypeId = typeId }
        };

        _repositoryMock.Setup(r => r.GetByIdAsync<Contact>(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, CancellationToken ct) => mockContacts.FirstOrDefault(c => c.Id == id));

        _repositoryMock.Setup(r => r.ListAsNoTrackingAsync<Contact>(
                It.IsAny<Expression<Func<Contact, bool>>>(), It.IsAny<CancellationToken>(), It.IsAny<string[]>()))
            .ReturnsAsync((Expression<Func<Contact, bool>> expr, CancellationToken ct, string[] includes) =>
                mockContacts.AsQueryable().Where(expr).ToList());

        _repositoryMock.Setup(r => r.ListAsNoTrackingAsync<Relationship>(
                It.IsAny<Expression<Func<Relationship, bool>>>(), It.IsAny<CancellationToken>(), It.IsAny<string[]>()))
            .ReturnsAsync((Expression<Func<Relationship, bool>> expr, CancellationToken ct, string[] includes) =>
                mockRelationships.AsQueryable().Where(expr).ToList());

        // Act
        List<SuggestedRelationshipDto> result = await _service.GetSuggestedRelationshipsAsync(
            entityId, relatedEntityId, typeId, false, null);

        // Assert
        Assert.NotEmpty(result);
        Assert.Contains(result, s => s.SourceName == "Other A" && s.TargetName == "Related User");
        Assert.Contains(result, s => s.SourceName == "Main User" && s.TargetName == "Other B");
    }
}
