using Rvnx.CRM.Core.DTOs.Contact;
using Rvnx.CRM.Core.Extensions;
using Rvnx.CRM.Core.Models.Contact;
using Xunit;

namespace Rvnx.CRM.Tests.Extensions
{
    public class PetDtoMappingTests
    {
        [Fact]
        public void ToDtoShouldMapPropertiesCorrectly()
        {
            // Arrange
            var entity = new Pet
            {
                Id = Guid.NewGuid(),
                Name = "Buddy",
                Species = "Dog",
                Breed = "Golden Retriever",
                Birthday = new DateTime(2020, 1, 1),
                Notes = "Loves tennis balls",
                ContactId = Guid.NewGuid()
            };

            // Act
            var dto = entity.ToDto();

            // Assert
            Assert.Equal(entity.Id, dto.Id);
            Assert.Equal(entity.Name, dto.Name);
            Assert.Equal(entity.Species, dto.Species);
            Assert.Equal(entity.Breed, dto.Breed);
            Assert.Equal(entity.Birthday, dto.Birthday);
            Assert.Equal(entity.Notes, dto.Notes);
            Assert.Equal(entity.ContactId, dto.EntityId);

            // ToDto does not set EntityType, so it should be empty/null as initialized in DTO
            Assert.True(string.IsNullOrEmpty(dto.EntityType));
        }

        [Fact]
        public void ToEntityShouldCreateNewPetWithCorrectProperties()
        {
            // Arrange
            var dto = new PetFormDto
            {
                Name = "Mittens",
                Species = "Cat",
                Breed = "Siamese",
                Birthday = new DateTime(2019, 5, 15),
                Notes = "Hates water",
                EntityId = Guid.NewGuid()
            };

            // Act
            var entity = dto.ToEntity();

            // Assert
            Assert.NotEqual(Guid.Empty, entity.Id);
            Assert.Equal(dto.Name, entity.Name);
            Assert.Equal(dto.Species, entity.Species);
            Assert.Equal(dto.Breed, entity.Breed);
            Assert.Equal(dto.Birthday, entity.Birthday);
            Assert.Equal(dto.Notes, entity.Notes);
            Assert.Equal(dto.EntityId, entity.ContactId);
        }

        [Fact]
        public void UpdateEntityShouldUpdatePropertiesCorrectly()
        {
            // Arrange
            var initialContactId = Guid.NewGuid();
            var entity = new Pet
            {
                Id = Guid.NewGuid(),
                Name = "Old Name",
                Species = "Old Species",
                Breed = "Old Breed",
                Birthday = DateTime.MinValue,
                Notes = "Old Notes",
                ContactId = initialContactId
            };

            var dto = new PetFormDto
            {
                Name = "New Name",
                Species = "New Species",
                Breed = "New Breed",
                Birthday = DateTime.UtcNow,
                Notes = "New Notes",
                // EntityId in DTO might be different but should be ignored by UpdateEntity
                EntityId = Guid.NewGuid()
            };

            // Act
            entity.UpdateEntity(dto);

            // Assert
            Assert.Equal(dto.Name, entity.Name);
            Assert.Equal(dto.Species, entity.Species);
            Assert.Equal(dto.Breed, entity.Breed);
            Assert.Equal(dto.Birthday, entity.Birthday);
            Assert.Equal(dto.Notes, entity.Notes);

            // Verify ContactId remains unchanged
            Assert.Equal(initialContactId, entity.ContactId);
        }
    }
}
