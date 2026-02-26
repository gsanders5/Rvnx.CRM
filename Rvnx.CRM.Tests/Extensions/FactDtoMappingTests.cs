using Rvnx.CRM.Core.Constants;
using Rvnx.CRM.Core.DTOs.Contact;
using Rvnx.CRM.Core.Extensions;
using Rvnx.CRM.Core.Models.Contact;

namespace Rvnx.CRM.Tests.Extensions
{
    public class FactDtoMappingTests
    {
        [Fact]
        public void ToDtoShouldMapPropertiesCorrectly()
        {
            // Arrange
            var fact = new Fact
            {
                Id = Guid.NewGuid(),
                Category = "Test Category",
                Value = "Test Value",
                ContactId = Guid.NewGuid(),
                CreatedDate = DateTime.UtcNow
            };

            // Act
            var dto = fact.ToDto();

            // Assert
            Assert.Equal(fact.Id, dto.Id);
            Assert.Equal(fact.Category, dto.Category);
            Assert.Equal(fact.Value, dto.Value);
            Assert.Equal(fact.ContactId.Value, dto.EntityId);
            Assert.Equal(EntityTypes.Person, dto.EntityType);
            Assert.Equal(fact.CreatedDate, dto.CreatedDate);
        }

        [Fact]
        public void ToDtoShouldMapNullContactIdToEmptyGuid()
        {
            // Arrange
            var fact = new Fact
            {
                Id = Guid.NewGuid(),
                Category = "Test Category",
                Value = "Test Value",
                ContactId = null,
                CreatedDate = DateTime.UtcNow
            };

            // Act
            var dto = fact.ToDto();

            // Assert
            Assert.Equal(Guid.Empty, dto.EntityId);
        }

        [Fact]
        public void ToEntityShouldCreateNewFactWithCorrectProperties()
        {
            // Arrange
            var formDto = new FactFormDto
            {
                Category = "New Category",
                Value = "New Value",
                EntityId = Guid.NewGuid()
            };

            // Act
            var entity = formDto.ToEntity();

            // Assert
            Assert.NotEqual(Guid.Empty, entity.Id);
            Assert.Equal(formDto.Category, entity.Category);
            Assert.Equal(formDto.Value, entity.Value);
            Assert.Equal(formDto.EntityId, entity.ContactId);
        }

        [Fact]
        public void UpdateEntityShouldUpdatePropertiesCorrectly()
        {
            // Arrange
            var initialContactId = Guid.NewGuid();
            var fact = new Fact
            {
                Id = Guid.NewGuid(),
                Category = "Old Category",
                Value = "Old Value",
                ContactId = initialContactId
            };

            var formDto = new FactFormDto
            {
                Category = "Updated Category",
                Value = "Updated Value",
                // EntityId in DTO might be different but should be ignored by UpdateEntity
                EntityId = Guid.NewGuid()
            };

            // Act
            fact.UpdateEntity(formDto);

            // Assert
            Assert.Equal("Updated Category", fact.Category);
            Assert.Equal("Updated Value", fact.Value);

            // Verify ContactId remains unchanged
            Assert.Equal(initialContactId, fact.ContactId);
        }
    }
}
