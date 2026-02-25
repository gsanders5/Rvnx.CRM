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
            var person = new Contact
            {
                Id = Guid.NewGuid(),
                FirstName = "Father",
                Gender = "Male"
            };

            var relatedPerson = new Contact
            {
                Id = Guid.NewGuid(),
                FirstName = "Son",
                Gender = "Male"
            };

            var relationship = new Relationship
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
            var dto = relationship.ToDto();

            // Assert
            Assert.Equal("Father", dto.RelationshipTypeName);
            Assert.Equal("Son", dto.RelationshipTypeOppositeName);
        }

        [Fact]
        public void ToDtoShouldMapGenderSpecificNamesForSpouseRelationship()
        {
            // Arrange
            var husband = new Contact
            {
                Id = Guid.NewGuid(),
                FirstName = "Husband",
                Gender = "Male"
            };

            var wife = new Contact
            {
                Id = Guid.NewGuid(),
                FirstName = "Wife",
                Gender = "Female"
            };

            var relationship = new Relationship
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
            var dto = relationship.ToDto();

            // Assert
            Assert.Equal("Husband", dto.RelationshipTypeName);
            Assert.Equal("Wife", dto.RelationshipTypeOppositeName);
        }

        [Fact]
        public void ToDtoShouldMapGenderSpecificNamesForFemaleParentAndChild()
        {
            // Arrange
            var person = new Contact
            {
                Id = Guid.NewGuid(),
                FirstName = "Mother",
                Gender = "Female"
            };

            var relatedPerson = new Contact
            {
                Id = Guid.NewGuid(),
                FirstName = "Daughter",
                Gender = "Female"
            };

            var relationship = new Relationship
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
            var dto = relationship.ToDto();

            // Assert
            Assert.Equal("Mother", dto.RelationshipTypeName);
            Assert.Equal("Daughter", dto.RelationshipTypeOppositeName);
        }

        [Fact]
        public void ToDtoShouldMapNeutralNamesWhenGenderIsUnknownOrNonBinary()
        {
            // Arrange
            var person = new Contact
            {
                Id = Guid.NewGuid(),
                FirstName = "Parent",
                Gender = "Non-binary"
            };

            var relatedPerson = new Contact
            {
                Id = Guid.NewGuid(),
                FirstName = "Child",
                Gender = null // Unknown
            };

            var relationship = new Relationship
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
            var dto = relationship.ToDto();

            // Assert
            // "Parent" is the default name, "Child" is the default opposite name
            Assert.Equal("Parent", dto.RelationshipTypeName);
            Assert.Equal("Child", dto.RelationshipTypeOppositeName);
        }

        [Fact]
        public void ToDtoShouldMapConsistentNamesForGenderNeutralRelationships()
        {
            // Arrange
            var person = new Contact
            {
                Id = Guid.NewGuid(),
                FirstName = "Friend1",
                Gender = "Male"
            };

            var relatedPerson = new Contact
            {
                Id = Guid.NewGuid(),
                FirstName = "Friend2",
                Gender = "Female"
            };

            var relationship = new Relationship
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
            var dto = relationship.ToDto();

            // Assert
            // Friend/Friend regardless of gender
            Assert.Equal("Friend", dto.RelationshipTypeName);
            Assert.Equal("Friend", dto.RelationshipTypeOppositeName);
        }

        [Fact]
        public void ToDtoShouldHandleNullPersonReferencesGracefully()
        {
            // Arrange
            var relationship = new Relationship
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
            var dto = relationship.ToDto();

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
            var relationship = new Relationship
            {
                Id = Guid.NewGuid(),
                RelationshipTypeId = Guid.NewGuid(), // Random ID not in service
                EntityId = Guid.NewGuid(),
                RelatedEntityId = Guid.NewGuid(),
                EntityType = "Person"
            };

            // Act
            var dto = relationship.ToDto();

            // Assert
            Assert.Equal("Unknown", dto.RelationshipTypeName);
            Assert.Equal("Unknown", dto.RelationshipTypeOppositeName);
        }
    }
}
