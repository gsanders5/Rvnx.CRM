using Moq;
using Rvnx.CRM.Core.Constants;
using Rvnx.CRM.Core.DTOs.Contact;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Core.Models.Dates;
using Rvnx.CRM.Core.Services;
using System.Linq.Expressions;

namespace Rvnx.CRM.Tests.Services
{
    public class RelationshipServiceTests
    {
        private readonly Mock<IRepository> _repositoryMock;
        private readonly RelationshipService _service;

        public RelationshipServiceTests()
        {
            _repositoryMock = new Mock<IRepository>();
            _service = new RelationshipService(_repositoryMock.Object);
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
}
