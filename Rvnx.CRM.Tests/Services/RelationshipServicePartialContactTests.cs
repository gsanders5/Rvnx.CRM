using Moq;
using Rvnx.CRM.Core.Constants;
using Rvnx.CRM.Core.DTOs.Contact;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Core.Models.Dates;
using Rvnx.CRM.Core.Services;

namespace Rvnx.CRM.Tests.Services
{
    public class RelationshipServicePartialContactTests
    {
        private readonly Mock<IRepository> _repositoryMock;
        private readonly RelationshipService _service;

        public RelationshipServicePartialContactTests()
        {
            _repositoryMock = new Mock<IRepository>();
            _service = new RelationshipService(_repositoryMock.Object);
        }

        [Fact]
        public async Task CreateRelationshipAsyncPartialContactSavesCorrectly()
        {
            // Arrange
            Guid entityId = Guid.NewGuid();
            Relationship relationship = new()
            {
                EntityId = entityId,
                RelatedEntityId = null, // Partial contact
                PartialContactFirstName = "John",
                PartialContactLastName = "Doe",
                EntityType = EntityTypes.Person
            };
            Guid typeId = Guid.NewGuid();
            string selectedType = $"{typeId}_Fwd";

            // Act
            RelationshipOperationResult result = await _service.CreateRelationshipAsync(relationship, selectedType);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(typeId, relationship.RelationshipTypeId);
            Assert.Equal(entityId, relationship.EntityId);
            Assert.Null(relationship.RelatedEntityId);
            Assert.Equal("John", relationship.PartialContactFirstName);
            Assert.Equal(entityId, result.RedirectId);

            _repositoryMock.Verify(r => r.AddAsync(relationship, It.IsAny<CancellationToken>()), Times.Once);
            _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task CreateRelationshipAsyncPartialContactMissingNameReturnsError()
        {
            // Arrange
            Relationship relationship = new()
            {
                EntityId = Guid.NewGuid(),
                RelatedEntityId = null, // Partial contact
                PartialContactFirstName = "", // Invalid
                EntityType = EntityTypes.Person
            };
            Guid typeId = Guid.NewGuid();
            string selectedType = $"{typeId}_Fwd";

            // Act
            RelationshipOperationResult result = await _service.CreateRelationshipAsync(relationship, selectedType);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("First Name is required for partial contacts.", result.ErrorMessage);
            _repositoryMock.Verify(r => r.AddAsync(It.IsAny<Relationship>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task CreateRelationshipAsyncPartialContactReverseSetsIsTypeReverse()
        {
            // Arrange
            Guid entityId = Guid.NewGuid();
            Relationship relationship = new()
            {
                EntityId = entityId,
                RelatedEntityId = null, // Partial contact
                PartialContactFirstName = "John",
                EntityType = EntityTypes.Person
            };
            Guid typeId = Guid.NewGuid();
            string selectedType = $"{typeId}_Rev"; // Reverse

            // Act
            RelationshipOperationResult result = await _service.CreateRelationshipAsync(relationship, selectedType);

            // Assert
            Assert.True(result.Success);
            Assert.True(relationship.IsTypeReverse); // Should be marked as reverse
            Assert.Equal(entityId, relationship.EntityId);
            Assert.Null(relationship.RelatedEntityId);
            Assert.Equal(entityId, result.RedirectId);

            _repositoryMock.Verify(r => r.AddAsync(relationship, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task PromotePartialContactCreatesContactAndLinksRelationship()
        {
            // Arrange
            Guid relationshipId = Guid.NewGuid();
            Relationship relationship = new()
            {
                Id = relationshipId,
                EntityId = Guid.NewGuid(),
                RelatedEntityId = null,
                PartialContactFirstName = "John",
                PartialContactLastName = "Doe",
                PartialContactDateOfBirth = new DateTime(2000, 1, 1),
                EntityType = EntityTypes.Person
            };

            _repositoryMock.Setup(r => r.GetByIdAsync<Relationship>(relationshipId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(relationship);

            // Capture the contact being added
            Contact? capturedContact = null;
            _repositoryMock.Setup(r => r.AddAsync(It.IsAny<Contact>(), It.IsAny<CancellationToken>()))
                .Callback<Contact, CancellationToken>((c, t) => capturedContact = c)
                .ReturnsAsync((Contact c, CancellationToken t) => c);

            // Capture birthday being added
            SignificantDate? capturedBirthday = null;
            _repositoryMock.Setup(r => r.AddAsync(It.IsAny<SignificantDate>(), It.IsAny<CancellationToken>()))
                .Callback<SignificantDate, CancellationToken>((d, t) => capturedBirthday = d)
                .ReturnsAsync((SignificantDate d, CancellationToken t) => d);

            // Act
            RelationshipOperationResult result = await _service.PromotePartialContactAsync(relationshipId);

            // Assert
            Assert.True(result.Success);

            Assert.NotNull(capturedContact);
            Assert.Equal("John", capturedContact.FirstName);
            Assert.Equal("Doe", capturedContact.LastName);

            Assert.NotNull(capturedBirthday);
            Assert.Equal(capturedContact.Id, capturedBirthday.ContactId);
            Assert.Equal(SignificantDateTitles.Birthday, capturedBirthday.Title);
            Assert.Equal(new DateTime(2000, 1, 1), capturedBirthday.Date);

            // Relationship updated
            Assert.Equal(capturedContact.Id, relationship.RelatedEntityId);
            Assert.Null(relationship.PartialContactFirstName);
            Assert.Null(relationship.PartialContactLastName);
            Assert.Null(relationship.PartialContactDateOfBirth);

            _repositoryMock.Verify(r => r.UpdateAsync(relationship, It.IsAny<CancellationToken>()), Times.Once);
            _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task PromotePartialContactReverseTypeSwapsEntities()
        {
            // Arrange
            Guid relationshipId = Guid.NewGuid();
            Guid entityId = Guid.NewGuid();
            Relationship relationship = new()
            {
                Id = relationshipId,
                EntityId = entityId,
                RelatedEntityId = null,
                PartialContactFirstName = "John",
                IsTypeReverse = true, // Was reverse
                EntityType = EntityTypes.Person
            };

            _repositoryMock.Setup(r => r.GetByIdAsync<Relationship>(relationshipId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(relationship);

            Contact? capturedContact = null;
            _repositoryMock.Setup(r => r.AddAsync(It.IsAny<Contact>(), It.IsAny<CancellationToken>()))
                .Callback<Contact, CancellationToken>((c, t) => capturedContact = c)
                .ReturnsAsync((Contact c, CancellationToken t) => c);

            // Act
            RelationshipOperationResult result = await _service.PromotePartialContactAsync(relationshipId);

            // Assert
            Assert.True(result.Success);

            Assert.False(relationship.IsTypeReverse);
            Assert.Equal(capturedContact!.Id, relationship.EntityId); // Swapped: EntityId is now John
            Assert.Equal(entityId, relationship.RelatedEntityId); // Swapped: RelatedEntityId is now original entity

            _repositoryMock.Verify(r => r.UpdateAsync(relationship, It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
