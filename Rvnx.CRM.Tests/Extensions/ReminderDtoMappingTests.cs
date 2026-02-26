using Rvnx.CRM.Core.Constants;
using Rvnx.CRM.Core.DTOs.Dates;
using Rvnx.CRM.Core.Extensions;
using Rvnx.CRM.Core.Models.Dates;
using System;
using Xunit;

namespace Rvnx.CRM.Tests.Extensions
{
    public class ReminderDtoMappingTests
    {
        [Fact]
        public void ToDtoShouldMapAllPropertiesWhenEntityIsFullyPopulated()
        {
            // Arrange
            var entity = new Reminder
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
            var dto = entity.ToDto();

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
            var entity = new Reminder
            {
                ContactId = null,
                DueDate = DateTime.Now
            };

            // Act
            var dto = entity.ToDto();

            // Assert
            Assert.Equal(Guid.Empty, dto.EntityId);
        }

        [Fact]
        public void ToDtoShouldMapNullDescriptionAsNull()
        {
            // Arrange
            var entity = new Reminder
            {
                Description = null,
                DueDate = DateTime.Now
            };

            // Act
            var dto = entity.ToDto();

            // Assert
            Assert.Null(dto.Description);
        }

        [Fact]
        public void ToDtoShouldSetEntityTypeToPerson()
        {
            // Arrange
            var entity = new Reminder
            {
                DueDate = DateTime.Now
            };

            // Act
            var dto = entity.ToDto();

            // Assert
            Assert.Equal(EntityTypes.Person, dto.EntityType);
        }
    }
}
