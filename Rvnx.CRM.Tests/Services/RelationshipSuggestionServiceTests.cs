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
    public async Task GetSuggestedRelationshipsAsync_WhenContactDoesNotExist_ReturnsEmpty()
    {
        Guid entityId = Guid.NewGuid();
        Guid typeId = RelationshipTypeIds.Sibling;

        _repositoryMock.Setup(r => r.ListProjectedAsync(
                It.IsAny<Expression<Func<Contact, bool>>>(),
                It.IsAny<Expression<Func<Contact, string>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        List<SuggestedRelationshipDto> result =
            await _service.GetSuggestedRelationshipsAsync(entityId, null, typeId, false, null);

        Assert.Empty(result);
        _repositoryMock.Verify(
            r => r.ListAsNoTrackingAsync<Relationship>(
                It.IsAny<Expression<Func<Relationship, bool>>>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "Test names follow a standard convention")]
    public async Task GetSuggestedRelationshipsAsync_WithTransitiveType_ReturnsSuggestionsForRelatedComponent()
    {
        Guid contactId = Guid.NewGuid();
        Guid relatedContactId = Guid.NewGuid();
        Guid newTransitiveContactId = Guid.NewGuid();
        Guid typeId = RelationshipTypeIds.Sibling;

        Contact contact = new() { Id = contactId, FirstName = "John", LastName = "Doe" };
        Contact relatedContact = new() { Id = relatedContactId, FirstName = "Jane", LastName = "Doe" };
        Contact newContact = new() { Id = newTransitiveContactId, FirstName = "Jim", LastName = "Doe" };

        List<Contact> contactsDb = [contact, relatedContact, newContact];

        _repositoryMock.Setup(r => r.ListProjectedAsync(
                It.IsAny<Expression<Func<Contact, bool>>>(),
                It.IsAny<Expression<Func<Contact, string>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Expression<Func<Contact, bool>> expr, Expression<Func<Contact, string>> selector, CancellationToken ct) =>
                contactsDb.AsQueryable().Where(expr).Select(selector).ToList());

        List<Relationship> relationshipsDb = [
            new() { ContactId = relatedContactId, RelatedContactId = newTransitiveContactId, RelationshipTypeId = typeId }
        ];

        _repositoryMock.Setup(r => r.ListAsNoTrackingAsync<Relationship>(
                It.IsAny<Expression<Func<Relationship, bool>>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<string[]>()))
            .ReturnsAsync((Expression<Func<Relationship, bool>> expr, CancellationToken ct, string[] includes) =>
                relationshipsDb.AsQueryable().Where(expr).ToList());


        _repositoryMock.Setup(r => r.ListProjectedAsync(
                It.IsAny<Expression<Func<Contact, bool>>>(),
                It.IsAny<Expression<Func<Contact, ValueTuple<Guid, string>>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Expression<Func<Contact, bool>> expr, Expression<Func<Contact, ValueTuple<Guid, string>>> selector, CancellationToken ct) =>
                contactsDb.AsQueryable().Where(expr).Select(selector).ToList());

        List<SuggestedRelationshipDto> result =
            await _service.GetSuggestedRelationshipsAsync(contactId, relatedContactId, typeId, false, null);

        Assert.Single(result);
        SuggestedRelationshipDto suggestion = result.First();
        Assert.Equal("John Doe", suggestion.SourceName);
        Assert.Equal("Jim Doe", suggestion.TargetName);
        Assert.Equal("Sibling", suggestion.RelationshipName);
    }

    [Fact]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "Test names follow a standard convention")]
    public async Task GetSuggestedRelationshipsAsync_WithFamilyAdultChildType_ReturnsSuggestionsForChildSiblings()
    {
        Guid adultId = Guid.NewGuid();
        Guid childId = Guid.NewGuid();
        Guid siblingId = Guid.NewGuid();
        Guid typeId = RelationshipTypeIds.Parent; // FamilyAdultChild

        Contact adult = new() { Id = adultId, FirstName = "Adult", LastName = "One" };
        Contact child = new() { Id = childId, FirstName = "Child", LastName = "One" };
        Contact sibling = new() { Id = siblingId, FirstName = "Sibling", LastName = "One" };

        List<Contact> contactsDb = [adult, child, sibling];

        _repositoryMock.Setup(r => r.ListProjectedAsync(
                It.IsAny<Expression<Func<Contact, bool>>>(),
                It.IsAny<Expression<Func<Contact, string>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Expression<Func<Contact, bool>> expr, Expression<Func<Contact, string>> selector, CancellationToken ct) =>
                contactsDb.AsQueryable().Where(expr).Select(selector).ToList());

        List<Relationship> relationshipsDb = [
            new() { ContactId = childId, RelatedContactId = siblingId, RelationshipTypeId = RelationshipTypeIds.Sibling }
        ];

        _repositoryMock.Setup(r => r.ListAsNoTrackingAsync<Relationship>(
                It.IsAny<Expression<Func<Relationship, bool>>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<string[]>()))
            .ReturnsAsync((Expression<Func<Relationship, bool>> expr, CancellationToken ct, string[] includes) =>
                relationshipsDb.AsQueryable().Where(expr).ToList());

        _repositoryMock.Setup(r => r.ListProjectedAsync(
                It.IsAny<Expression<Func<Contact, bool>>>(),
                It.IsAny<Expression<Func<Contact, ValueTuple<Guid, string>>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Expression<Func<Contact, bool>> expr, Expression<Func<Contact, ValueTuple<Guid, string>>> selector, CancellationToken ct) =>
                contactsDb.AsQueryable().Where(expr).Select(selector).ToList());

        List<SuggestedRelationshipDto> result =
            await _service.GetSuggestedRelationshipsAsync(adultId, childId, typeId, false, null);

        Assert.Single(result);
        SuggestedRelationshipDto suggestion = result.First();
        Assert.Equal("Adult One", suggestion.SourceName);
        Assert.Equal("Sibling One", suggestion.TargetName);
        Assert.Equal("Parent", suggestion.RelationshipName); // Note: It suggests adult -> sibling relation.
    }
}
