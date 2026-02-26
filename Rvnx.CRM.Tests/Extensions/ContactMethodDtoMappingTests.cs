using Rvnx.CRM.Core.Constants;
using Rvnx.CRM.Core.DTOs.Contact;
using Rvnx.CRM.Core.Enumerations;
using Rvnx.CRM.Core.Extensions;
using Rvnx.CRM.Core.Models.Contact;
using Xunit;

namespace Rvnx.CRM.Tests.Extensions
{
    public class ContactMethodDtoMappingTests
    {
        [Fact]
        public void ToDtoShouldMapPropertiesCorrectly()
        {
            // Arrange
            var entity = new ContactMethod
            {
                Id = Guid.NewGuid(),
                Type = ContactMethodType.Email,
                Value = "test@example.com",
                Label = "Work",
                ContactId = Guid.NewGuid(),
                CreatedDate = DateTime.UtcNow
            };

            // Act
            var dto = entity.ToDto();

            // Assert
            Assert.Equal(entity.Id, dto.Id);
            Assert.Equal(entity.Type, dto.Type);
            Assert.Equal(entity.Value, dto.Value);
            Assert.Equal(entity.Label, dto.Label);
            Assert.Equal(entity.ContactId.Value, dto.EntityId);
            Assert.Equal(EntityTypes.Person, dto.EntityType);
            Assert.Equal(entity.CreatedDate, dto.CreatedDate);
        }

        [Fact]
        public void ToEntityShouldCreateNewContactMethodWithCorrectProperties()
        {
            // Arrange
            var dto = new ContactMethodFormDto
            {
                Type = ContactMethodType.Phone,
                Value = "+1234567890",
                Label = "Mobile",
                EntityId = Guid.NewGuid()
            };

            // Act
            var entity = dto.ToEntity();

            // Assert
            Assert.NotEqual(Guid.Empty, entity.Id);
            Assert.Equal(dto.Type, entity.Type);
            Assert.Equal(dto.Value, entity.Value);
            Assert.Equal(dto.Label, entity.Label);
            Assert.Equal(dto.EntityId, entity.ContactId);
        }

        [Fact]
        public void UpdateEntityShouldUpdatePropertiesCorrectly()
        {
            // Arrange
            var initialContactId = Guid.NewGuid();
            var entity = new ContactMethod
            {
                Id = Guid.NewGuid(),
                Type = ContactMethodType.Email,
                Value = "old@example.com",
                Label = "Old Label",
                ContactId = initialContactId
            };

            var dto = new ContactMethodFormDto
            {
                Type = ContactMethodType.Website,
                Value = "https://example.com",
                Label = "New Label",
                // EntityId in DTO might be different but should be ignored by UpdateEntity
                EntityId = Guid.NewGuid()
            };

            // Act
            entity.UpdateEntity(dto);

            // Assert
            Assert.Equal(dto.Type, entity.Type);
            Assert.Equal(dto.Value, entity.Value);
            Assert.Equal(dto.Label, entity.Label);

            // Verify ContactId remains unchanged
            Assert.Equal(initialContactId, entity.ContactId);
        }
    }
}
