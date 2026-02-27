using Rvnx.CRM.Core.Constants;
using Rvnx.CRM.Core.DTOs.Dates;
using Rvnx.CRM.Core.Extensions;
using Rvnx.CRM.Core.Models.Dates;

namespace Rvnx.CRM.Tests.Extensions
{
    public class ReminderDtoMappingTests
    {
        [Fact]
        public void ToDtoShouldMapAllPropertiesWhenEntityIsFullyPopulated()
        {
            // Arrange
            Reminder entity = new()
            {
                Id = Guid.NewGuid(),
                Title = "Meeting",
                DueDate = new DateTime(2023, 10, 27),
                Description = "Discuss project",
                ContactId = Guid.NewGuid(),
                RemindMe = true,
                ReminderSent = DateTime.UtcNow.AddDays(-1),
                EventFrequency = TimeSpan.FromDays(7),
                IsCompleted = true
            };

            // Act
            ReminderDto dto = entity.ToDto();

            // Assert
            Assert.Equal(entity.Id, dto.Id);
            Assert.Equal(entity.Title, dto.Title);
            Assert.Equal(entity.DueDate, dto.DueDate);
            Assert.Equal(entity.Description, dto.Description);
            Assert.Equal(entity.ContactId, dto.EntityId);
            Assert.Equal(EntityTypes.Person, dto.EntityType);
            Assert.Equal(entity.RemindMe, dto.RemindMe);
            Assert.Equal(entity.ReminderSent, dto.ReminderSent);
            Assert.Equal(entity.EventFrequency, dto.EventFrequency);
            Assert.Equal(entity.IsCompleted, dto.IsCompleted);
        }

        [Fact]
        public void ToDtoShouldMapNullContactIdToEmptyGuid()
        {
            // Arrange
            Reminder entity = new()
            {
                ContactId = null,
                DueDate = DateTime.Now
            };

            // Act
            ReminderDto dto = entity.ToDto();

            // Assert
            Assert.Equal(Guid.Empty, dto.EntityId);
        }

        [Fact]
        public void ToDtoShouldMapNullDescriptionAsNull()
        {
            // Arrange
            Reminder entity = new()
            {
                Description = null,
                DueDate = DateTime.Now
            };

            // Act
            ReminderDto dto = entity.ToDto();

            // Assert
            Assert.Null(dto.Description);
        }

        [Fact]
        public void ToDtoShouldSetEntityTypeToPerson()
        {
            // Arrange
            Reminder entity = new()
            {
                DueDate = DateTime.Now
            };

            // Act
            ReminderDto dto = entity.ToDto();

            // Assert
            Assert.Equal(EntityTypes.Person, dto.EntityType);
        }

        [Fact]
        public void UpdateEntityShouldUpdatePropertiesCorrectly()
        {
            // Arrange
            Guid initialContactId = Guid.NewGuid();
            Reminder entity = new()
            {
                Id = Guid.NewGuid(),
                Title = "Original Title",
                Description = "Original Description",
                DueDate = new DateTime(2023, 1, 1),
                IsCompleted = false,
                RemindMe = false,
                EventFrequency = TimeSpan.Zero,
                ContactId = initialContactId
            };

            ReminderDto dto = new()
            {
                Title = "Updated Title",
                Description = "Updated Description",
                DueDate = new DateTime(2023, 12, 31),
                IsCompleted = true,
                RemindMe = true,
                EventFrequency = TimeSpan.FromDays(1)
            };

            // Act
            entity.UpdateEntity(dto);

            // Assert
            Assert.Equal(dto.Title, entity.Title);
            Assert.Equal(dto.Description, entity.Description);
            Assert.Equal(dto.DueDate, entity.DueDate);
            Assert.Equal(dto.IsCompleted, entity.IsCompleted);
            Assert.Equal(dto.RemindMe, entity.RemindMe);
            Assert.Equal(dto.EventFrequency, entity.EventFrequency);

            // Verify unchanged properties
            Assert.Equal(initialContactId, entity.ContactId);
        }
    }
}
