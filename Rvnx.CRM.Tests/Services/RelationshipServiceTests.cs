using Moq;
using Rvnx.CRM.Core.Constants;
using Rvnx.CRM.Core.DTOs.Common;
using Rvnx.CRM.Core.DTOs.Contact;
using Rvnx.CRM.Core.Enumerations;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Core.Models.Dates;
using Rvnx.CRM.Core.Services;
using System.Linq.Expressions;

namespace Rvnx.CRM.Tests.Services;

public class RelationshipServiceTests
{
    private readonly Mock<IRepository> _repositoryMock;
    private readonly RelationshipService _service;
    private readonly RelationshipSuggestionService _suggestionService;

    public RelationshipServiceTests()
    {
        _repositoryMock = new Mock<IRepository>();
        _suggestionService = new RelationshipSuggestionService(_repositoryMock.Object);
        _service = new RelationshipService(_repositoryMock.Object, _suggestionService);
    }

    [Fact]
    public async Task GetRelatedContactOptionsAsyncWhenEntityTypeIsPersonReturnsPersonOptions()
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

        List<SelectOptionDto> result = await _service.GetRelatedContactOptionsAsync(entityId);

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
    public async Task GetRelatedContactOptionsAsyncPersonProjectionAppendsPartialAndDeceasedSuffixes()
    {
        Guid entityId = Guid.NewGuid();

        Expression<Func<Contact, SelectOptionDto>>? capturedProjection = null;

        _repositoryMock.Setup(r => r.ListProjectedAsync<Contact, SelectOptionDto, string>(
                It.IsAny<Expression<Func<Contact, bool>>>(),
                It.IsAny<Expression<Func<Contact, SelectOptionDto>>>(),
                It.IsAny<Expression<Func<Contact, string>>>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .Callback<Expression<Func<Contact, bool>>, Expression<Func<Contact, SelectOptionDto>>, Expression<Func<Contact, string>>, bool, CancellationToken>(
                (_, projection, _, _, _) => capturedProjection = projection)
            .ReturnsAsync([]);

        await _service.GetRelatedContactOptionsAsync(entityId);

        Assert.NotNull(capturedProjection);
        Func<Contact, SelectOptionDto> projectionFunc = capturedProjection.Compile();

        Contact full = new() { Id = Guid.NewGuid(), FirstName = "John", LastName = "Doe" };
        Contact partial = new() { Id = Guid.NewGuid(), FirstName = "Jane", LastName = "Smith", IsPartial = true };
        Contact deceased = new() { Id = Guid.NewGuid(), FirstName = "Late", LastName = "Person", IsDeceased = true };
        Contact partialDeceased = new() { Id = Guid.NewGuid(), FirstName = "Ghost", LastName = "Soul", IsPartial = true, IsDeceased = true };

        Assert.Equal("John Doe", projectionFunc(full).Text);
        Assert.Equal("Jane Smith (Partial Contact)", projectionFunc(partial).Text);
        Assert.Equal("Late Person (Deceased)", projectionFunc(deceased).Text);
        Assert.Equal("Ghost Soul (Partial Contact, Deceased)", projectionFunc(partialDeceased).Text);
    }

    [Fact]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "Test names follow standard convention")]
    public void GetRelationshipTypeOptions_Symmetric_ReturnsOneOption()
    {
        // (No arrange needed as we are using the statically populated list of relationship types)

        List<SelectOptionDto> result = _service.GetRelationshipTypeOptions();

        List<SelectOptionDto> spouseOptions = result.Where(x => x.Value.StartsWith(RelationshipTypeIds.Spouse.ToString(), StringComparison.Ordinal)).ToList();

        Assert.Single(spouseOptions);
        Assert.Equal($"{RelationshipTypeIds.Spouse}_Fwd", spouseOptions[0].Value);
        Assert.Equal("is Spouse of", spouseOptions[0].Text);
        Assert.Equal("Romantic", spouseOptions[0].Group);
        Assert.False(spouseOptions[0].Selected);
    }

    [Fact]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "Test names follow standard convention")]
    public void GetRelationshipTypeOptions_Asymmetric_ReturnsTwoOptions()
    {
        // (No arrange needed as we are using the statically populated list of relationship types)

        List<SelectOptionDto> result = _service.GetRelationshipTypeOptions();

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

        List<SelectOptionDto> result = _service.GetRelationshipTypeOptions(selectedValue);

        List<SelectOptionDto> parentOptions = result.Where(x => x.Value.StartsWith(RelationshipTypeIds.Parent.ToString(), StringComparison.Ordinal)).ToList();

        Assert.Equal(2, parentOptions.Count);
        Assert.False(parentOptions.Single(x => x.Value.EndsWith("_Fwd", StringComparison.Ordinal)).Selected);
        Assert.True(parentOptions.Single(x => x.Value.EndsWith("_Rev", StringComparison.Ordinal)).Selected);
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
    public async Task GetRelationshipForEditAsyncWhenFoundReturnsRelationship()
    {
        Guid relationshipId = Guid.NewGuid();
        Relationship relationship = new() { Id = relationshipId };

        _repositoryMock.Setup(r => r.GetByIdAsync<Relationship>(relationshipId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(relationship);

        Relationship? result = await _service.GetRelationshipForEditAsync(relationshipId);

        Assert.NotNull(result);
        Assert.Equal(relationshipId, result.Id);
        _repositoryMock.Verify(r => r.GetByIdAsync<Relationship>(relationshipId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetRelationshipForEditAsyncWhenNotFoundReturnsNull()
    {
        Guid relationshipId = Guid.NewGuid();

        _repositoryMock.Setup(r => r.GetByIdAsync<Relationship>(relationshipId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Relationship?)null);

        Relationship? result = await _service.GetRelationshipForEditAsync(relationshipId);

        Assert.Null(result);
        _repositoryMock.Verify(r => r.GetByIdAsync<Relationship>(relationshipId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("InvalidFormat")]
    [InlineData("NotAGuid_Fwd")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores",
        Justification = "Test names follow a standard convention")]
    public async Task UpdateRelationshipAsync_WithInvalidSelection_ReturnsFailure(string? invalidSelection)
    {
        Relationship relationship = new();

        RelationshipOperationResult
            result = await _service.UpdateRelationshipAsync(Guid.NewGuid(), relationship, invalidSelection!);

        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
        _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Relationship>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores",
        Justification = "Test names follow a standard convention")]
    public async Task UpdateRelationshipAsync_WhenNotFound_ReturnsFailure()
    {
        Guid relationshipId = Guid.NewGuid();
        string selection = $"{Guid.NewGuid()}_Fwd";
        Relationship updatedRelationship = new();

        _repositoryMock.Setup(r => r.GetByIdAsync<Relationship>(relationshipId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Relationship?)null);

        RelationshipOperationResult result = await _service.UpdateRelationshipAsync(relationshipId, updatedRelationship, selection);

        Assert.False(result.Success);
        Assert.Equal("Relationship not found.", result.ErrorMessage);
    }

    [Fact]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores",
        Justification = "Test names follow a standard convention")]
    public async Task UpdateRelationshipAsync_WhenDuplicateExists_ReturnsFailure()
    {
        Guid relationshipId = Guid.NewGuid();
        Guid typeId = Guid.NewGuid();
        string selection = $"{typeId}_Fwd";

        Relationship existingRelationship = new() { Id = relationshipId, ContactId = Guid.NewGuid(), RelatedContactId = Guid.NewGuid() };
        Relationship updatedRelationship = new() { ContactId = existingRelationship.ContactId, RelatedContactId = existingRelationship.RelatedContactId };

        _repositoryMock.Setup(r => r.GetByIdAsync<Relationship>(relationshipId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingRelationship);

        _repositoryMock.Setup(r => r.CountAsync<Relationship>(
            It.IsAny<System.Linq.Expressions.Expression<Func<Relationship, bool>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(1); // Simulating a duplicate exists

        RelationshipOperationResult result = await _service.UpdateRelationshipAsync(relationshipId, updatedRelationship, selection);

        Assert.False(result.Success);
        Assert.Contains("This exact relationship already exists", result.ErrorMessage);
        _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Relationship>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores",
        Justification = "Test names follow a standard convention")]
    public async Task UpdateRelationshipAsync_WithValidForwardSelection_UpdatesWithoutSwapping()
    {
        Guid relationshipId = Guid.NewGuid();
        Guid typeId = Guid.NewGuid();
        string selection = $"{typeId}_Fwd";
        Guid entityId = Guid.NewGuid();
        Guid relatedEntityId = Guid.NewGuid();

        Relationship existingRelationship = new() { Id = relationshipId, ContactId = Guid.NewGuid(), RelatedContactId = Guid.NewGuid() };
        Relationship updatedRelationship = new()
        {
            ContactId = entityId,
            RelatedContactId = relatedEntityId,
            Description = "Updated description"
        };

        _repositoryMock.Setup(r => r.GetByIdAsync<Relationship>(relationshipId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingRelationship);

        _repositoryMock.Setup(r => r.CountAsync<Relationship>(
            It.IsAny<System.Linq.Expressions.Expression<Func<Relationship, bool>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        RelationshipOperationResult result = await _service.UpdateRelationshipAsync(relationshipId, updatedRelationship, selection);

        Assert.True(result.Success);
        Assert.Equal(entityId, result.RedirectId);

        Assert.Equal(typeId, existingRelationship.RelationshipTypeId);
        Assert.Equal(entityId, existingRelationship.ContactId);
        Assert.Equal(relatedEntityId, existingRelationship.RelatedContactId);
        Assert.Equal("Updated description", existingRelationship.Description);

        _repositoryMock.Verify(r => r.UpdateAsync(existingRelationship, It.IsAny<CancellationToken>()), Times.Once);
        _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores",
        Justification = "Test names follow a standard convention")]
    public async Task UpdateRelationshipAsync_WithValidReverseSelection_SwapsEntities()
    {
        Guid relationshipId = Guid.NewGuid();
        Guid typeId = Guid.NewGuid();
        string selection = $"{typeId}_Rev";
        Guid entityId = Guid.NewGuid();
        Guid relatedEntityId = Guid.NewGuid();

        Relationship existingRelationship = new() { Id = relationshipId, ContactId = Guid.NewGuid(), RelatedContactId = Guid.NewGuid() };
        Relationship updatedRelationship = new()
        {
            ContactId = entityId,
            RelatedContactId = relatedEntityId
        };

        _repositoryMock.Setup(r => r.GetByIdAsync<Relationship>(relationshipId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingRelationship);

        _repositoryMock.Setup(r => r.CountAsync<Relationship>(
            It.IsAny<System.Linq.Expressions.Expression<Func<Relationship, bool>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        RelationshipOperationResult result = await _service.UpdateRelationshipAsync(relationshipId, updatedRelationship, selection);

        Assert.True(result.Success);
        Assert.Equal(entityId, result.RedirectId);

        Assert.Equal(typeId, existingRelationship.RelationshipTypeId);
        Assert.Equal(relatedEntityId, existingRelationship.ContactId); // Swapped
        Assert.Equal(entityId, existingRelationship.RelatedContactId); // Swapped

        _repositoryMock.Verify(r => r.UpdateAsync(existingRelationship, It.IsAny<CancellationToken>()), Times.Once);
        _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
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
        _repositoryMock.Setup(r => r.ListProjectedAsync(
                It.Is<Expression<Func<Contact, bool>>>(expr => expr.Compile().Invoke(new Contact { Id = sourceId })),
                It.IsAny<Expression<Func<Contact, string>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(["Jack"]);

        _repositoryMock.Setup(r => r.ListProjectedAsync(
                It.Is<Expression<Func<Contact, bool>>>(expr => expr.Compile().Invoke(new Contact { Id = targetId })),
                It.IsAny<Expression<Func<Contact, string>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(["Jill"]);

        // BFS edge query: target is connected to cId via the Colleague relationship
        _repositoryMock.Setup(r => r.ListAsNoTrackingAsync(
                It.IsAny<Expression<Func<Relationship, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new Relationship { ContactId = targetId, RelatedContactId = cId, RelationshipTypeId = typeId }
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

        List<SuggestedRelationshipDto> suggestions = await _suggestionService.GetSuggestedRelationshipsAsync(sourceId, targetId, typeId, false, null);

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
            ContactId = entityId,
            RelatedContactId = relatedEntityId
        };

        RelationshipOperationResult result = await _service.CreateRelationshipAsync(relationship, selection);

        Assert.True(result.Success);
        Assert.Equal(entityId, result.RedirectId);

        Assert.Equal(typeId, relationship.RelationshipTypeId);
        Assert.Equal(entityId, relationship.ContactId);
        Assert.Equal(relatedEntityId, relationship.RelatedContactId);

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
            ContactId = entityId,
            RelatedContactId = relatedEntityId
        };

        RelationshipOperationResult result = await _service.CreateRelationshipAsync(relationship, selection);

        Assert.True(result.Success);
        Assert.Equal(entityId,
            result.RedirectId); // The original entity stays the primary entity because Swap is called before Ok

        Assert.Equal(typeId, relationship.RelationshipTypeId);
        Assert.Equal(relatedEntityId, relationship.ContactId); // Swapped
        Assert.Equal(entityId, relationship.RelatedContactId); // Swapped

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

        _repositoryMock.Setup(r => r.GetByIdAsync<Contact>(parentEntityId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Contact { Id = parentEntityId, FirstName = "Parent" });

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
            rel.ContactId == parentEntityId &&
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
        Guid contactId = Guid.NewGuid();

        _repositoryMock.Setup(r => r.ListProjectedAsync<Relationship, Guid>(
                It.IsAny<Expression<Func<Relationship, bool>>>(),
                It.IsAny<Expression<Func<Relationship, Guid>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([contactId]);

        OperationResult result = await _service.DeleteRelationshipAsync(relationshipId);

        Assert.True(result.Success);
        Assert.Equal(contactId, result.RedirectId);

        _repositoryMock.Verify(r => r.DeleteAsync(It.IsAny<Expression<Func<Relationship, bool>>>(), It.IsAny<CancellationToken>()), Times.Once);
        _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores",
        Justification = "Test names follow a standard convention")]
    public async Task DeleteRelationshipAsyncWhenNotFoundReturnsFailure()
    {
        Guid relationshipId = Guid.NewGuid();

        _repositoryMock.Setup(r => r.ListProjectedAsync<Relationship, Guid>(
                It.IsAny<Expression<Func<Relationship, bool>>>(),
                It.IsAny<Expression<Func<Relationship, Guid>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        OperationResult result = await _service.DeleteRelationshipAsync(relationshipId);

        Assert.False(result.Success);
        Assert.Equal("Relationship not found.", result.ErrorMessage);

        _repositoryMock.Verify(r => r.DeleteAsync(It.IsAny<Expression<Func<Relationship, bool>>>(), It.IsAny<CancellationToken>()), Times.Never);
        _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateRelationshipAsyncSymmetricTypeForwardDoesNotSwapEntities()
    {
        Guid typeId = RelationshipTypeIds.Spouse;
        Guid entityIdA = Guid.NewGuid();
        Guid entityIdB = Guid.NewGuid();

        Relationship relationship = new()
        {
            ContactId = entityIdA,
            RelatedContactId = entityIdB
        };

        RelationshipOperationResult result =
            await _service.CreateRelationshipAsync(relationship, $"{typeId}_Fwd");

        Assert.True(result.Success);
        Assert.Equal(typeId, relationship.RelationshipTypeId);
        Assert.Equal(entityIdA, relationship.ContactId);
        Assert.Equal(entityIdB, relationship.RelatedContactId);

        _repositoryMock.Verify(r => r.AddAsync(relationship, It.IsAny<CancellationToken>()), Times.Once);
        _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateRelationshipAsyncSymmetricTypeReverseSwapsToCanonicalDirection()
    {
        // Spouse is symmetric — _Rev still swaps ContactId/RelatedContactId so canonical storage is consistent
        Guid typeId = RelationshipTypeIds.Spouse;
        Guid entityIdA = Guid.NewGuid();
        Guid entityIdB = Guid.NewGuid();

        Relationship relationship = new()
        {
            ContactId = entityIdB,
            RelatedContactId = entityIdA
        };

        RelationshipOperationResult result =
            await _service.CreateRelationshipAsync(relationship, $"{typeId}_Rev");

        Assert.True(result.Success);
        Assert.Equal(typeId, relationship.RelationshipTypeId);
        Assert.Equal(entityIdA, relationship.ContactId);
        Assert.Equal(entityIdB, relationship.RelatedContactId);

        _repositoryMock.Verify(r => r.AddAsync(relationship, It.IsAny<CancellationToken>()), Times.Once);
        _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreatePartialContactRelationshipAsyncValidatesPartialContactIdReplacement()
    {
        Guid parentEntityId = Guid.NewGuid();
        Guid typeId = Guid.NewGuid();
        string selection = $"{typeId}_Fwd";

        string suggestionPayload = $"{Guid.Empty}_{Guid.NewGuid()}_False";

        CreatePartialContactRelationshipDto dto = new()
        {
            PartialContactFirstName = "Tom",
            PartialContactLastName = "Smith",
            Description = "Partial contact suggestion test",
            SuggestedRelationships = [suggestionPayload]
        };

        Guid capturedSuggestedEntityId = Guid.Empty;
        _repositoryMock.Setup(r => r.AddAsync(It.IsAny<Relationship>(), It.IsAny<CancellationToken>()))
            .Callback<Relationship, CancellationToken>((rel, _) =>
            {
                if (rel.Description == "Automatically added from suggested relationship.")
                {
                    capturedSuggestedEntityId = rel.ContactId;
                }
            })
            .ReturnsAsync((Relationship rel, CancellationToken _) => rel);

        _repositoryMock.Setup(r => r.ListAsNoTrackingAsync<Relationship>(
                It.IsAny<Expression<Func<Relationship, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        _repositoryMock.Setup(r => r.GetByIdAsync<Contact>(parentEntityId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Contact { Id = parentEntityId, FirstName = "Parent" });

        RelationshipOperationResult result =
            await _service.CreatePartialContactRelationshipAsync(parentEntityId, selection, dto);

        Assert.True(result.Success);
        Assert.NotEqual(Guid.Empty, capturedSuggestedEntityId);
    }

    [Fact]
    public async Task CreatePartialContactRelationshipAsyncReturnsFailureWhenParentContactNotVisible()
    {
        Guid parentEntityId = Guid.NewGuid();
        Guid typeId = Guid.NewGuid();
        string selection = $"{typeId}_Fwd";

        CreatePartialContactRelationshipDto dto = new()
        {
            PartialContactFirstName = "Jane"
        };

        _repositoryMock.Setup(r => r.GetByIdAsync<Contact>(parentEntityId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Contact?)null);

        RelationshipOperationResult result =
            await _service.CreatePartialContactRelationshipAsync(parentEntityId, selection, dto);

        Assert.False(result.Success);
        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<Contact>(), It.IsAny<CancellationToken>()), Times.Never);
        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<Relationship>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateRelationshipAsyncWithDuplicateRejectsBeforePersistence()
    {
        Guid typeId = Guid.NewGuid();
        string selection = $"{typeId}_Fwd";
        Guid entityId = Guid.NewGuid();
        Guid relatedEntityId = Guid.NewGuid();

        Relationship relationship = new()
        {
            ContactId = entityId,
            RelatedContactId = relatedEntityId
        };

        _repositoryMock.Setup(r => r.CountAsync<Relationship>(
                It.IsAny<Expression<Func<Relationship, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        RelationshipOperationResult result = await _service.CreateRelationshipAsync(relationship, selection);

        Assert.False(result.Success);
        Assert.Contains("already exists", result.ErrorMessage);

        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<Relationship>(), It.IsAny<CancellationToken>()), Times.Never);
        _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
