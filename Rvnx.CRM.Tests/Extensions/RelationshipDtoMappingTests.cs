using Rvnx.CRM.Core.DTOs.Contact;
using Rvnx.CRM.Core.Extensions;
using Rvnx.CRM.Core.Models.Contact;

namespace Rvnx.CRM.Tests.Extensions
{
    public class RelationshipDtoMappingTests
    {
        // IDs from RelationshipTypeService
        private static readonly Guid ParentRelationshipId = Guid.Parse("7c1f8d22-1b6a-4c28-9c1e-3f5a2b8e9d1a");
        private static readonly Guid FriendRelationshipId = Guid.Parse("a5b6c7d8-9e0f-1a2b-3c4d-5e6f7a8b9c0d");
        private static readonly Guid SpouseRelationshipId = Guid.Parse("b2e9a5c8-7f4d-4a1b-8c6e-5f9d3a0e2b4c");

        [Fact]
        public void ToDtoShouldMapGenderSpecificNamesForMaleParentAndChild()
        {
            // Arrange
            Contact person = new()
            {
                Id = Guid.NewGuid(),
                FirstName = "Father",
                Gender = Rvnx.CRM.Core.Constants.PersonalAttributeOptions.Male
            };

            Contact relatedPerson = new()
            {
                Id = Guid.NewGuid(),
                FirstName = "Son",
                Gender = Rvnx.CRM.Core.Constants.PersonalAttributeOptions.Male
            };

            Relationship relationship = new()
            {
                Id = Guid.NewGuid(),
                RelationshipTypeId = ParentRelationshipId,
                Person = person,
                RelatedPerson = relatedPerson,
                EntityId = person.Id,
                RelatedEntityId = relatedPerson.Id,
                EntityType = "Person"
            };

            // Act
            RelationshipDto dto = relationship.ToDto();

            // Assert
            Assert.Equal("Father", dto.RelationshipTypeName);
            Assert.Equal("Son", dto.RelationshipTypeOppositeName);
        }

        [Fact]
        public void ToDtoShouldMapGenderSpecificNamesForSpouseRelationship()
        {
            // Arrange
            Contact husband = new()
            {
                Id = Guid.NewGuid(),
                FirstName = "Husband",
                Gender = Rvnx.CRM.Core.Constants.PersonalAttributeOptions.Male
            };

            Contact wife = new()
            {
                Id = Guid.NewGuid(),
                FirstName = "Wife",
                Gender = Rvnx.CRM.Core.Constants.PersonalAttributeOptions.Female
            };

            Relationship relationship = new()
            {
                Id = Guid.NewGuid(),
                RelationshipTypeId = SpouseRelationshipId,
                Person = husband,
                RelatedPerson = wife,
                EntityId = husband.Id,
                RelatedEntityId = wife.Id,
                EntityType = "Person"
            };

            // Act
            RelationshipDto dto = relationship.ToDto();

            // Assert
            Assert.Equal("Husband", dto.RelationshipTypeName);
            Assert.Equal("Wife", dto.RelationshipTypeOppositeName);
        }

        [Fact]
        public void ToDtoShouldMapGenderSpecificNamesForFemaleParentAndChild()
        {
            // Arrange
            Contact person = new()
            {
                Id = Guid.NewGuid(),
                FirstName = "Mother",
                Gender = Rvnx.CRM.Core.Constants.PersonalAttributeOptions.Female
            };

            Contact relatedPerson = new()
            {
                Id = Guid.NewGuid(),
                FirstName = "Daughter",
                Gender = Rvnx.CRM.Core.Constants.PersonalAttributeOptions.Female
            };

            Relationship relationship = new()
            {
                Id = Guid.NewGuid(),
                RelationshipTypeId = ParentRelationshipId,
                Person = person,
                RelatedPerson = relatedPerson,
                EntityId = person.Id,
                RelatedEntityId = relatedPerson.Id,
                EntityType = "Person"
            };

            // Act
            RelationshipDto dto = relationship.ToDto();

            // Assert
            Assert.Equal("Mother", dto.RelationshipTypeName);
            Assert.Equal("Daughter", dto.RelationshipTypeOppositeName);
        }

        [Fact]
        public void ToDtoShouldMapNeutralNamesWhenGenderIsUnknownOrNonBinary()
        {
            // Arrange
            Contact person = new()
            {
                Id = Guid.NewGuid(),
                FirstName = "Parent",
                Gender = "Non-binary"
            };

            Contact relatedPerson = new()
            {
                Id = Guid.NewGuid(),
                FirstName = "Child",
                Gender = null // Unknown
            };

            Relationship relationship = new()
            {
                Id = Guid.NewGuid(),
                RelationshipTypeId = ParentRelationshipId,
                Person = person,
                RelatedPerson = relatedPerson,
                EntityId = person.Id,
                RelatedEntityId = relatedPerson.Id,
                EntityType = "Person"
            };

            // Act
            RelationshipDto dto = relationship.ToDto();

            // Assert
            // "Parent" is the default name, "Child" is the default opposite name
            Assert.Equal("Parent", dto.RelationshipTypeName);
            Assert.Equal("Child", dto.RelationshipTypeOppositeName);
        }

        [Fact]
        public void ToDtoShouldMapConsistentNamesForGenderNeutralRelationships()
        {
            // Arrange
            Contact person = new()
            {
                Id = Guid.NewGuid(),
                FirstName = "Friend1",
                Gender = Rvnx.CRM.Core.Constants.PersonalAttributeOptions.Male
            };

            Contact relatedPerson = new()
            {
                Id = Guid.NewGuid(),
                FirstName = "Friend2",
                Gender = Rvnx.CRM.Core.Constants.PersonalAttributeOptions.Female
            };

            Relationship relationship = new()
            {
                Id = Guid.NewGuid(),
                RelationshipTypeId = FriendRelationshipId,
                Person = person,
                RelatedPerson = relatedPerson,
                EntityId = person.Id,
                RelatedEntityId = relatedPerson.Id,
                EntityType = "Person"
            };

            // Act
            RelationshipDto dto = relationship.ToDto();

            // Assert
            // Friend/Friend regardless of gender
            Assert.Equal("Friend", dto.RelationshipTypeName);
            Assert.Equal("Friend", dto.RelationshipTypeOppositeName);
        }

        [Fact]
        public void ToDtoShouldHandleNullPersonReferencesGracefully()
        {
            // Arrange
            Relationship relationship = new()
            {
                Id = Guid.NewGuid(),
                RelationshipTypeId = ParentRelationshipId,
                Person = null, // Should not happen in valid state, but testing robustness
                RelatedPerson = null,
                EntityId = Guid.NewGuid(),
                RelatedEntityId = Guid.NewGuid(),
                EntityType = "Person"
            };

            // Act
            RelationshipDto dto = relationship.ToDto();

            // Assert
            // Should fallback to default names since gender is null
            Assert.Equal("Parent", dto.RelationshipTypeName);
            Assert.Equal("Child", dto.RelationshipTypeOppositeName);
            Assert.Equal("Unknown", dto.EntityName);
            Assert.Equal("Unknown", dto.RelatedEntityName);
        }

        [Fact]
        public void ToDtoShouldReturnUnknownIfTypeNotFound()
        {
            // Arrange
            Relationship relationship = new()
            {
                Id = Guid.NewGuid(),
                RelationshipTypeId = Guid.NewGuid(), // Random ID not in service
                EntityId = Guid.NewGuid(),
                RelatedEntityId = Guid.NewGuid(),
                EntityType = "Person"
            };

            // Act
            RelationshipDto dto = relationship.ToDto();

            // Assert
            Assert.Equal("Unknown", dto.RelationshipTypeName);
            Assert.Equal("Unknown", dto.RelationshipTypeOppositeName);
        }

        [Fact]
        public void ToEntityShouldMapPropertiesCorrectly()
        {
            // Arrange
            RelationshipFormDto dto = new()
            {
                EntityId = Guid.NewGuid(),
                RelatedEntityId = Guid.NewGuid(),
                EntityType = "Person",
                RelationshipTypeId = Guid.NewGuid(),
                Description = "A description",
                StartDate = DateTime.Now.Date,
                EndDate = DateTime.Now.Date.AddYears(1)
            };

            // Act
            Relationship entity = dto.ToEntity();

            // Assert
            Assert.NotEqual(Guid.Empty, entity.Id);
            Assert.Equal(dto.EntityId, entity.EntityId);
            Assert.Equal(dto.RelatedEntityId, entity.RelatedEntityId);
            Assert.Equal(dto.EntityType, entity.EntityType);
            Assert.Equal(dto.RelationshipTypeId, entity.RelationshipTypeId);
            Assert.Equal(dto.Description, entity.Description);
            Assert.Equal(dto.StartDate, entity.StartDate);
            Assert.Equal(dto.EndDate, entity.EndDate);
        }

        [Fact]
        public void UpdateEntityShouldUpdatePropertiesCorrectly()
        {
            // Arrange
            Relationship entity = new()
            {
                Id = Guid.NewGuid(),
                EntityId = Guid.NewGuid(),
                RelatedEntityId = Guid.NewGuid(),
                RelationshipTypeId = Guid.NewGuid(),
                Description = "Original description",
                StartDate = DateTime.Now.Date.AddDays(-10),
                EndDate = DateTime.Now.Date
            };

            RelationshipFormDto dto = new()
            {
                EntityId = Guid.NewGuid(), // Changed
                RelatedEntityId = Guid.NewGuid(), // Changed
                RelationshipTypeId = Guid.NewGuid(), // Changed
                Description = "Updated description",
                StartDate = DateTime.Now.Date.AddDays(-5),
                EndDate = DateTime.Now.Date.AddDays(5)
            };

            // Act
            entity.UpdateEntity(dto);

            // Assert
            Assert.Equal(dto.EntityId, entity.EntityId);
            Assert.Equal(dto.RelatedEntityId, entity.RelatedEntityId);
            Assert.Equal(dto.RelationshipTypeId, entity.RelationshipTypeId);
            Assert.Equal(dto.Description, entity.Description);
            Assert.Equal(dto.StartDate, entity.StartDate);
            Assert.Equal(dto.EndDate, entity.EndDate);
        }

        [Fact]
        public void UpdateEntityShouldHandleNullValues()
        {
            // Arrange
            Relationship entity = new()
            {
                Id = Guid.NewGuid(),
                EntityId = Guid.NewGuid(),
                RelatedEntityId = Guid.NewGuid(),
                RelationshipTypeId = Guid.NewGuid(),
                Description = "Original description",
                StartDate = DateTime.Now.Date,
                EndDate = DateTime.Now.Date
            };

            RelationshipFormDto dto = new()
            {
                EntityId = entity.EntityId,
                RelatedEntityId = entity.RelatedEntityId,
                RelationshipTypeId = entity.RelationshipTypeId,
                Description = null,
                StartDate = null,
                EndDate = null
            };

            // Act
            entity.UpdateEntity(dto);

            // Assert
            Assert.Null(entity.Description);
            Assert.Null(entity.StartDate);
            Assert.Null(entity.EndDate);
        }

        [Fact]
        public void UpdateEntityShouldNotModifyIdOrEntityType()
        {
            // Arrange
            Guid originalId = Guid.NewGuid();
            string originalEntityType = "OriginalType";

            Relationship entity = new()
            {
                Id = originalId,
                EntityId = Guid.NewGuid(),
                RelatedEntityId = Guid.NewGuid(),
                RelationshipTypeId = Guid.NewGuid(),
                EntityType = originalEntityType
            };

            RelationshipFormDto dto = new()
            {
                // Different ID and EntityType in DTO (though DTO ID is nullable and EntityType usually ignored in Update)
                Id = Guid.NewGuid(),
                EntityType = "DifferentType",
                EntityId = Guid.NewGuid(),
                RelatedEntityId = Guid.NewGuid(),
                RelationshipTypeId = Guid.NewGuid()
            };

            // Act
            entity.UpdateEntity(dto);

            // Assert
            Assert.Equal(originalId, entity.Id);
            Assert.Equal(originalEntityType, entity.EntityType);
        }

        [Fact]
        public void UpdateEntityShouldUpdateForeignKeys()
        {
            // Arrange
            Relationship entity = new()
            {
                Id = Guid.NewGuid(),
                EntityId = Guid.NewGuid(),
                RelatedEntityId = Guid.NewGuid(),
                RelationshipTypeId = Guid.NewGuid()
            };

            RelationshipFormDto dto = new()
            {
                EntityId = Guid.NewGuid(), // Changed
                RelatedEntityId = Guid.NewGuid(), // Changed
                RelationshipTypeId = Guid.NewGuid() // Changed
            };

            // Act
            entity.UpdateEntity(dto);

            // Assert
            Assert.Equal(dto.EntityId, entity.EntityId);
            Assert.Equal(dto.RelatedEntityId, entity.RelatedEntityId);
            Assert.Equal(dto.RelationshipTypeId, entity.RelationshipTypeId);
        }
    }
}
