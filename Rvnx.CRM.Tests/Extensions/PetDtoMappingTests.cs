using Rvnx.CRM.Core.DTOs.Contact;
using Rvnx.CRM.Core.Extensions;
using Rvnx.CRM.Core.Models.Contact;
using System;
using Xunit;

namespace Rvnx.CRM.Tests.Extensions
{
    public class PetDtoMappingTests
    {
        [Fact]
        public void ToDtoShouldMapPropertiesCorrectly()
        {
            // Arrange
            var pet = new Pet
            {
                Id = Guid.NewGuid(),
                Name = "Buddy",
                Species = "Dog",
                Breed = "Golden Retriever",
                Birthday = new DateTime(2020, 1, 1),
                Notes = "Friendly dog",
                ContactId = Guid.NewGuid()
            };

            // Act
            var dto = pet.ToDto();

            // Assert
            Assert.Equal(pet.Id, dto.Id);
            Assert.Equal(pet.Name, dto.Name);
            Assert.Equal(pet.Species, dto.Species);
            Assert.Equal(pet.Breed, dto.Breed);
            Assert.Equal(pet.Birthday, dto.Birthday);
            Assert.Equal(pet.Notes, dto.Notes);
            Assert.Equal(pet.ContactId, dto.EntityId);
        }

        [Fact]
        public void ToEntityShouldCreateNewPetWithCorrectProperties()
        {
            // Arrange
            var formDto = new PetFormDto
            {
                Name = "Mittens",
                Species = "Cat",
                Breed = "Siamese",
                Birthday = new DateTime(2021, 5, 10),
                Notes = "Likes to scratch",
                EntityId = Guid.NewGuid()
            };

            // Act
            var entity = formDto.ToEntity();

            // Assert
            Assert.NotEqual(Guid.Empty, entity.Id);
            Assert.Equal(formDto.Name, entity.Name);
            Assert.Equal(formDto.Species, entity.Species);
            Assert.Equal(formDto.Breed, entity.Breed);
            Assert.Equal(formDto.Birthday, entity.Birthday);
            Assert.Equal(formDto.Notes, entity.Notes);
            Assert.Equal(formDto.EntityId, entity.ContactId);
        }

        [Fact]
        public void UpdateEntityShouldUpdatePropertiesCorrectly()
        {
            // Arrange
            var initialContactId = Guid.NewGuid();
            var pet = new Pet
            {
                Id = Guid.NewGuid(),
                Name = "Old Name",
                Species = "Old Species",
                Breed = "Old Breed",
                Birthday = new DateTime(2019, 1, 1),
                Notes = "Old Notes",
                ContactId = initialContactId
            };

            var formDto = new PetFormDto
            {
                Name = "New Name",
                Species = "New Species",
                Breed = "New Breed",
                Birthday = new DateTime(2022, 1, 1),
                Notes = "New Notes",
                // EntityId in DTO might be different but should be ignored by UpdateEntity
                EntityId = Guid.NewGuid()
            };

            // Act
            pet.UpdateEntity(formDto);

            // Assert
            Assert.Equal(formDto.Name, pet.Name);
            Assert.Equal(formDto.Species, pet.Species);
            Assert.Equal(formDto.Breed, pet.Breed);
            Assert.Equal(formDto.Birthday, pet.Birthday);
            Assert.Equal(formDto.Notes, pet.Notes);

            // Verify ContactId remains unchanged
            Assert.Equal(initialContactId, pet.ContactId);
        }
    }
}
