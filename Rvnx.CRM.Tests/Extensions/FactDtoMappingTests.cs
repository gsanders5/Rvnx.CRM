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
            Fact fact = new()
            {
                Id = Guid.NewGuid(),
                Category = "Test Category",
                Value = "Test Value",
                ContactId = Guid.NewGuid(),
                CreatedDate = DateTime.UtcNow
            };

            FactDto dto = fact.ToDto();

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
            Fact fact = new()
            {
                Id = Guid.NewGuid(),
                Category = "Test Category",
                Value = "Test Value",
                ContactId = null,
                CreatedDate = DateTime.UtcNow
            };

            FactDto dto = fact.ToDto();

            Assert.Equal(Guid.Empty, dto.EntityId);
        }

        [Fact]
        public void ToEntityShouldCreateNewFactWithCorrectProperties()
        {
            FactFormDto formDto = new()
            {
                Category = "New Category",
                Value = "New Value",
                EntityId = Guid.NewGuid()
            };

            Fact entity = formDto.ToEntity();

            Assert.NotEqual(Guid.Empty, entity.Id);
            Assert.Equal(formDto.Category, entity.Category);
            Assert.Equal(formDto.Value, entity.Value);
            Assert.Equal(formDto.EntityId, entity.ContactId);
        }

        [Fact]
        public void UpdateEntityShouldUpdatePropertiesCorrectly()
        {
            Guid initialContactId = Guid.NewGuid();
            Fact fact = new()
            {
                Id = Guid.NewGuid(),
                Category = "Old Category",
                Value = "Old Value",
                ContactId = initialContactId
            };

            FactFormDto formDto = new()
            {
                Category = "Updated Category",
                Value = "Updated Value",
                // EntityId in DTO might be different but should be ignored by UpdateEntity
                EntityId = Guid.NewGuid()
            };

            fact.UpdateEntity(formDto);

            Assert.Equal("Updated Category", fact.Category);
            Assert.Equal("Updated Value", fact.Value);

            Assert.Equal(initialContactId, fact.ContactId);
        }
    }
}