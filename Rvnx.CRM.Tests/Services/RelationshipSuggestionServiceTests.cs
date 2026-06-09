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
}
