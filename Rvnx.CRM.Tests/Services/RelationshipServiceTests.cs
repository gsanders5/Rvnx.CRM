using Moq;
using Rvnx.CRM.Core.Constants;
using Rvnx.CRM.Core.DTOs.Common;
using Rvnx.CRM.Core.DTOs.Contact;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models;
using Rvnx.CRM.Core.Models.Business;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Core.Models.Dates;
using Rvnx.CRM.Core.Services;
using System.Linq.Expressions;

namespace Rvnx.CRM.Tests.Services;

public class RelationshipServiceTests
{
    private readonly Mock<IRepository> _repositoryMock;
    private readonly RelationshipService _service;

    public RelationshipServiceTests()
    {
        _repositoryMock = new Mock<IRepository>();
        _service = new RelationshipService(_repositoryMock.Object);
    }

    [Fact]
    public async Task GetRelatedEntityOptionsAsyncWhenEntityTypeIsPersonReturnsPersonOptions()
    {
        Guid entityId = Guid.NewGuid();
        Guid otherContactId = Guid.NewGuid();

        _repositoryMock.Setup(r => r.ListProjectedAsync<Contact, SelectOptionDto, string>(
                It.IsAny<System.Linq.Expressions.Expression<Func<Contact, bool>>>(),
                It.IsAny<System.Linq.Expressions.Expression<Func<Contact, SelectOptionDto>>>(),
                It.IsAny<System.Linq.Expressions.Expression<Func<Contact, string>>>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new SelectOptionDto { Value = otherContactId.ToString(), Text = "John Doe" }
            ]);

        List<SelectOptionDto> result = await _service.GetRelatedEntityOptionsAsync(entityId, EntityTypes.Person);

        Assert.Single(result);
        Assert.Equal("John Doe", result[0].Text);
        Assert.Equal(otherContactId.ToString(), result[0].Value);

        _repositoryMock.Verify(r => r.ListProjectedAsync<Contact, SelectOptionDto, string>(
                It.IsAny<System.Linq.Expressions.Expression<Func<Contact, bool>>>(),
                It.IsAny<System.Linq.Expressions.Expression<Func<Contact, SelectOptionDto>>>(),
                It.IsAny<System.Linq.Expressions.Expression<Func<Contact, string>>>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "Test names follow standard convention")]
    public void GetRelationshipTypeOptions_Symmetric_ReturnsOneOption()
    {
        // (No arrange needed as we are using the statically populated list of relationship types)

        List<SelectOptionDto> result = _service.GetRelationshipTypeOptions(EntityTypes.Person);

        List<SelectOptionDto> spouseOptions = result.Where(x => x.Value.StartsWith(RelationshipTypeIds.Spouse.ToString(), StringComparison.Ordinal)).ToList();

        Assert.Single(spouseOptions);
        Assert.Equal($"{RelationshipTypeIds.Spouse}_Fwd", spouseOptions[0].Value);
        Assert.Equal("is Spouse of", spouseOptions[0].Text);
        Assert.Equal("Family", spouseOptions[0].Group);
        Assert.False(spouseOptions[0].Selected);
    }

    [Fact]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "Test names follow standard convention")]
    public void GetRelationshipTypeOptions_Asymmetric_ReturnsTwoOptions()
    {
        // (No arrange needed as we are using the statically populated list of relationship types)

        List<SelectOptionDto> result = _service.GetRelationshipTypeOptions(EntityTypes.Person);

        List<SelectOptionDto> parentOptions = result.Where(x => x.Value.StartsWith(RelationshipTypeIds.Parent.ToString(), StringComparison.Ordinal)).ToList();

        Assert.Equal(2, parentOptions.Count);

        SelectOptionDto fwdOption = parentOptions.Single(x => x.Value.EndsWith("_Fwd", StringComparison.Ordinal));
        Assert.Equal($"{RelationshipTypeIds.Parent}_Fwd", fwdOption.Value);
        Assert.Equal("is Parent of (Child)", fwdOption.Text);
        Assert.Equal("Family", fwdOption.Group);
        Assert.False(fwdOption.Selected);

        SelectOptionDto revOption = parentOptions.Single(x => x.Value.EndsWith("_Rev", StringComparison.Ordinal));
        Assert.Equal($"{RelationshipTypeIds.Parent}_Rev", revOption.Value);
        Assert.Equal("is Child of (Parent)", revOption.Text);
        Assert.Equal("Family", revOption.Group);
        Assert.False(revOption.Selected);
    }

    [Fact]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "Test names follow standard convention")]
    public void GetRelationshipTypeOptions_WithSelectedValue_SetsSelectedFlag()
    {
        string selectedValue = $"{RelationshipTypeIds.Parent}_Rev";

        List<SelectOptionDto> result = _service.GetRelationshipTypeOptions(EntityTypes.Person, selectedValue);

        List<SelectOptionDto> parentOptions = result.Where(x => x.Value.StartsWith(RelationshipTypeIds.Parent.ToString(), StringComparison.Ordinal)).ToList();

        Assert.Equal(2, parentOptions.Count);
        Assert.False(parentOptions.Single(x => x.Value.EndsWith("_Fwd", StringComparison.Ordinal)).Selected);
        Assert.True(parentOptions.Single(x => x.Value.EndsWith("_Rev", StringComparison.Ordinal)).Selected);
    }

    [Fact]
    public async Task GetRelatedEntityOptionsAsyncWhenEntityTypeIsCompanyReturnsCompanyOptions()
    {
        Guid entityId = Guid.NewGuid();
        Guid companyId = Guid.NewGuid();

        _repositoryMock.Setup(r => r.ListProjectedAsync<Employer, SelectOptionDto, string>(
                It.IsAny<System.Linq.Expressions.Expression<Func<Employer, bool>>>(),
                It.IsAny<System.Linq.Expressions.Expression<Func<Employer, SelectOptionDto>>>(),
                It.IsAny<System.Linq.Expressions.Expression<Func<Employer, string>>>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new SelectOptionDto { Value = companyId.ToString(), Text = "Acme Corp" }
            ]);

        List<SelectOptionDto> result = await _service.GetRelatedEntityOptionsAsync(entityId, EntityTypes.Company);

        Assert.Single(result);
        Assert.Equal("Acme Corp", result[0].Text);
        Assert.Equal(companyId.ToString(), result[0].Value);

        _repositoryMock.Verify(r => r.ListProjectedAsync<Employer, SelectOptionDto, string>(
                It.IsAny<System.Linq.Expressions.Expression<Func<Employer, bool>>>(),
                It.IsAny<System.Linq.Expressions.Expression<Func<Employer, SelectOptionDto>>>(),
                It.IsAny<System.Linq.Expressions.Expression<Func<Employer, string>>>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData("UnknownType")]
    [InlineData(EntityTypes.Opportunity)]
    [InlineData(EntityTypes.Note)]
    [InlineData(EntityTypes.Attachment)]
    public async Task GetRelatedEntityOptionsAsyncWhenEntityTypeIsUnsupportedReturnsEmptyList(string entityType)
    {
        Guid entityId = Guid.NewGuid();

        List<SelectOptionDto> result = await _service.GetRelatedEntityOptionsAsync(entityId, entityType);

        Assert.Empty(result);
        Assert.Empty(_repositoryMock.Invocations);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("InvalidFormat")]
    [InlineData("NotAGuid_Fwd")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores",
        Justification = "Test names follow a standard convention")]
    public async Task CreateRelationshipAsync_WithInvalidSelection_ReturnsFailure(string? invalidSelection)
    {
        Relationship relationship = new();

        RelationshipOperationResult
            result = await _service.CreateRelationshipAsync(relationship, invalidSelection!);

        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<Relationship>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores",
        Justification = "Test names follow a standard convention")]
    public async Task GetSuggestedRelationshipsAsync_ReturnsSuggestions()
    {
        Guid sourceId = Guid.NewGuid();
        Guid targetId = Guid.NewGuid();
        Guid cId = Guid.NewGuid();
        Guid typeId = RelationshipTypeIds.Colleague;

        // Load the primary entity (source) and the directly-specified related entity (target)
        _repositoryMock.Setup(r => r.GetByIdAsync<Contact>(sourceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Contact { Id = sourceId, FirstName = "Jack" });

        _repositoryMock.Setup(r => r.GetByIdAsync<Contact>(targetId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Contact { Id = targetId, FirstName = "Jill" });

        // BFS edge query: target is connected to cId via the Colleague relationship
        _repositoryMock.Setup(r => r.ListAsNoTrackingAsync(
                It.IsAny<Expression<Func<Relationship, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new Relationship { EntityId = targetId, RelatedEntityId = cId, RelationshipTypeId = typeId }
            ]);

        // Batch contact load for BFS component nodes (cId is the discovered neighbour)
        _repositoryMock.Setup(r => r.ListAsNoTrackingAsync(
                It.IsAny<Expression<Func<Contact, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([new Contact { Id = cId, FirstName = "James" }]);

        // No existing relationships - explicitly mock to make the intent clear
        _repositoryMock.Setup(r => r.CountAsync(
                It.IsAny<Expression<Func<Relationship, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        List<SuggestedRelationshipDto> suggestions = await _service.GetSuggestedRelationshipsAsync(sourceId, targetId, typeId, false, null);

        SuggestedRelationshipDto? jackJamesSuggestion =
            suggestions.FirstOrDefault(s => s.SourceName == "Jack" && s.TargetName == "James");
        Assert.NotNull(jackJamesSuggestion);
        Assert.Equal($"{sourceId}_{cId}_False", jackJamesSuggestion.Payload);
    }

    [Fact]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores",
        Justification = "Test names follow a standard convention")]
    public async Task CreateRelationshipAsync_WithValidForwardSelection_SavesWithoutSwapping()
    {
        Guid typeId = Guid.NewGuid();
        string selection = $"{typeId}_Fwd";
        Guid entityId = Guid.NewGuid();
        Guid relatedEntityId = Guid.NewGuid();

        Relationship relationship = new()
        {
            EntityId = entityId,
            RelatedEntityId = relatedEntityId,
            EntityType = EntityTypes.Person
        };

        RelationshipOperationResult result = await _service.CreateRelationshipAsync(relationship, selection);

        Assert.True(result.Success);
        Assert.Equal(entityId, result.RedirectId);
        Assert.Equal(EntityTypes.Person, result.EntityType);

        Assert.Equal(typeId, relationship.RelationshipTypeId);
        Assert.Equal(entityId, relationship.EntityId);
        Assert.Equal(relatedEntityId, relationship.RelatedEntityId);

        _repositoryMock.Verify(r => r.AddAsync(relationship, It.IsAny<CancellationToken>()), Times.Once);
        _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores",
        Justification = "Test names follow a standard convention")]
    public async Task CreateRelationshipAsync_WithValidReverseSelection_SwapsEntities()
    {
        Guid typeId = Guid.NewGuid();
        string selection = $"{typeId}_Rev";
        Guid entityId = Guid.NewGuid();
        Guid relatedEntityId = Guid.NewGuid();

        Relationship relationship = new()
        {
            EntityId = entityId,
            RelatedEntityId = relatedEntityId,
            EntityType = EntityTypes.Person
        };

        RelationshipOperationResult result = await _service.CreateRelationshipAsync(relationship, selection);

        Assert.True(result.Success);
        Assert.Equal(entityId,
            result.RedirectId); // The original entity stays the primary entity because Swap is called before Ok
        Assert.Equal(EntityTypes.Person, result.EntityType);

        Assert.Equal(typeId, relationship.RelationshipTypeId);
        Assert.Equal(relatedEntityId, relationship.EntityId); // Swapped
        Assert.Equal(entityId, relationship.RelatedEntityId); // Swapped

        _repositoryMock.Verify(r => r.AddAsync(relationship, It.IsAny<CancellationToken>()), Times.Once);
        _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores",
        Justification = "Test names follow a standard convention")]
    public async Task CreatePartialContact_WithBirthday_CreatesContactDateAndRelationship()
    {
        Guid parentEntityId = Guid.NewGuid();
        Guid typeId = Guid.NewGuid();
        string selection = $"{typeId}_Fwd";

        CreatePartialContactRelationshipDto dto = new()
        {
            PartialContactFirstName = "Jane",
            PartialContactLastName = "Doe",
            Birthday = new DateTime(1990, 5, 15),
            Description = "A new partial contact"
        };

        RelationshipOperationResult result =
            await _service.CreatePartialContactRelationshipAsync(parentEntityId, selection, dto);

        Assert.True(result.Success);
        Assert.Equal(parentEntityId, result.RedirectId);

        _repositoryMock.Verify(r => r.AddAsync(It.Is<Contact>(c =>
            c.IsPartial &&
            c.FirstName == "Jane" &&
            c.LastName == "Doe"), It.IsAny<CancellationToken>()), Times.Once);

        _repositoryMock.Verify(r => r.AddAsync(It.Is<SignificantDate>(sd =>
            sd.Title == SignificantDateTitles.Birthday &&
            sd.EventDate == new DateOnly(1990, 5, 15)), It.IsAny<CancellationToken>()), Times.Once);

        _repositoryMock.Verify(r => r.AddAsync(It.Is<Relationship>(rel =>
            rel.EntityId == parentEntityId &&
            rel.RelationshipTypeId == typeId &&
            rel.Description == "A new partial contact"), It.IsAny<CancellationToken>()), Times.Once);

        _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores",
        Justification = "Test names follow a standard convention")]
    public async Task PromotePartialContact_WhenContactIsAlreadyPromoted_ReturnsFailure()
    {
        Guid contactId = Guid.NewGuid();
        Contact fullyPromotedContact = new() { Id = contactId, IsPartial = false };

        _repositoryMock.Setup(r => r.GetByIdAsync<Contact>(contactId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(fullyPromotedContact);

        RelationshipOperationResult result = await _service.PromotePartialContactAsync(contactId);

        Assert.False(result.Success);
        Assert.Equal("Contact is not a partial contact.", result.ErrorMessage);

        _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Contact>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores",
        Justification = "Test names follow a standard convention")]
    public async Task PromotePartialContact_WhenContactIsPartial_SetsIsPartialFalse()
    {
        Guid contactId = Guid.NewGuid();
        Contact partialContact = new() { Id = contactId, IsPartial = true };

        _repositoryMock.Setup(r => r.GetByIdAsync<Contact>(contactId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(partialContact);

        RelationshipOperationResult result = await _service.PromotePartialContactAsync(contactId);

        Assert.True(result.Success);
        Assert.Equal(contactId, result.RedirectId);
        Assert.False(partialContact.IsPartial);

        _repositoryMock.Verify(r => r.UpdateAsync(partialContact, It.IsAny<CancellationToken>()), Times.Once);
        _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores",
        Justification = "Test names follow a standard convention")]
    public async Task DeleteRelationshipAsyncWhenFoundReturnsOk()
    {
        Guid relationshipId = Guid.NewGuid();
        Guid entityId = Guid.NewGuid();
        string entityType = EntityTypes.Person;

        Relationship relationship = new()
        {
            Id = relationshipId,
            EntityId = entityId,
            EntityType = entityType
        };

        _repositoryMock.Setup(r => r.GetByIdAsync<Relationship>(relationshipId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(relationship);

        OperationResult result = await _service.DeleteRelationshipAsync(relationshipId);

        Assert.True(result.Success);
        Assert.Equal(entityId, result.RedirectId);
        Assert.Equal(entityType, result.RedirectType);

        _repositoryMock.Verify(r => r.DeleteAsync<Relationship>(relationshipId, It.IsAny<CancellationToken>()), Times.Once);
        _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores",
        Justification = "Test names follow a standard convention")]
    public async Task DeleteRelationshipAsyncWhenNotFoundReturnsFailure()
    {
        Guid relationshipId = Guid.NewGuid();

        _repositoryMock.Setup(r => r.GetByIdAsync<Relationship>(relationshipId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Relationship?)null);

        OperationResult result = await _service.DeleteRelationshipAsync(relationshipId);

        Assert.False(result.Success);
        Assert.Equal("Relationship not found.", result.ErrorMessage);

        _repositoryMock.Verify(r => r.DeleteAsync<Relationship>(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}