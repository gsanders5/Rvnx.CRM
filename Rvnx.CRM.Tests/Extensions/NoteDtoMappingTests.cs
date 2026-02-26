using Rvnx.CRM.Core.Constants;
using Rvnx.CRM.Core.DTOs.Base;
using Rvnx.CRM.Core.Extensions;
using Rvnx.CRM.Core.Models.Base;

namespace Rvnx.CRM.Tests.Extensions
{
    public class NoteDtoMappingTests
    {
        [Fact]
        public void ToDtoShouldMapPropertiesCorrectly()
        {
            // Arrange
            var note = new Note
            {
                Id = Guid.NewGuid(),
                Title = "Test Title",
                Value = "Test Value",
                ContactId = Guid.NewGuid(),
                CreatedDate = DateTime.UtcNow
            };

            // Act
            var dto = note.ToDto();

            // Assert
            Assert.Equal(note.Id, dto.Id);
            Assert.Equal(note.Title, dto.Title);
            Assert.Equal(note.Value, dto.Value);
            Assert.Equal(note.ContactId.Value, dto.EntityId);
            Assert.Equal(EntityTypes.Person, dto.EntityType);
            Assert.Equal(note.CreatedDate, dto.CreatedDate);
        }

        [Fact]
        public void ToDtoShouldMapNullContactIdToEmptyGuid()
        {
            // Arrange
            var note = new Note
            {
                Id = Guid.NewGuid(),
                Title = "Test Title",
                Value = "Test Value",
                ContactId = null,
                CreatedDate = DateTime.UtcNow
            };

            // Act
            var dto = note.ToDto();

            // Assert
            Assert.Equal(Guid.Empty, dto.EntityId);
        }

        [Fact]
        public void ToEntityShouldCreateNewNoteWithCorrectProperties()
        {
            // Arrange
            var formDto = new NoteFormDto
            {
                Title = "New Title",
                Value = "New Value",
                EntityId = Guid.NewGuid()
            };

            // Act
            var entity = formDto.ToEntity();

            // Assert
            Assert.NotEqual(Guid.Empty, entity.Id);
            Assert.Equal(formDto.Title, entity.Title);
            Assert.Equal(formDto.Value, entity.Value);
            Assert.Equal(formDto.EntityId, entity.ContactId);
        }

        [Fact]
        public void UpdateEntityShouldUpdatePropertiesCorrectly()
        {
            // Arrange
            var initialContactId = Guid.NewGuid();
            var note = new Note
            {
                Id = Guid.NewGuid(),
                Title = "Old Title",
                Value = "Old Value",
                ContactId = initialContactId
            };

            var formDto = new NoteFormDto
            {
                Title = "Updated Title",
                Value = "Updated Value",
                // EntityId in DTO might be different but should be ignored by UpdateEntity
                EntityId = Guid.NewGuid()
            };

            // Act
            note.UpdateEntity(formDto);

            // Assert
            Assert.Equal("Updated Title", note.Title);
            Assert.Equal("Updated Value", note.Value);

            // Verify ContactId remains unchanged
            Assert.Equal(initialContactId, note.ContactId);
        }
    }
}
