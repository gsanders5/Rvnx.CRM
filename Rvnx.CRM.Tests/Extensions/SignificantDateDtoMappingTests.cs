using Rvnx.CRM.Core.Constants;
using Rvnx.CRM.Core.DTOs.Dates;
using Rvnx.CRM.Core.Extensions;
using Rvnx.CRM.Core.Models.Dates;

namespace Rvnx.CRM.Tests.Extensions
{
    public class SignificantDateDtoMappingTests
    {
        [Fact]
        public void ToDtoShouldMapAllPropertiesWhenEntityIsFullyPopulated()
        {
            // Arrange
            SignificantDate entity = new()
            {
                Id = Guid.NewGuid(),
                Title = "Anniversary",
                Date = new DateTime(2023, 10, 27),
                Description = "A special day",
                ContactId = Guid.NewGuid(),
                RemindMe = true,
                ReminderSent = DateTime.UtcNow.AddDays(-1),
                EventFrequency = TimeSpan.FromDays(365)
            };

            // Act
            SignificantDateDto dto = entity.ToDto();

            // Assert
            Assert.Equal(entity.Id, dto.Id);
            Assert.Equal(entity.Title, dto.Title);
            Assert.Equal(entity.Date, dto.Date);
            Assert.Equal(entity.Description, dto.Description);
            Assert.Equal(entity.ContactId, dto.EntityId);
            Assert.Equal(EntityTypes.Person, dto.EntityType);
            Assert.Equal(entity.RemindMe, dto.RemindMe);
            Assert.Equal(entity.ReminderSent, dto.ReminderSent);
            Assert.Equal(entity.EventFrequency, dto.EventFrequency);
        }

        [Fact]
        public void ToDtoShouldMapNullTitleToEmptyString()
        {
            // Arrange
            SignificantDate entity = new()
            {
                Title = null,
                Date = DateTime.Now
            };

            // Act
            SignificantDateDto dto = entity.ToDto();

            // Assert
            Assert.Equal(string.Empty, dto.Title);
        }

        [Fact]
        public void ToDtoShouldMapNullContactIdToEmptyGuid()
        {
            // Arrange
            SignificantDate entity = new()
            {
                ContactId = null,
                Date = DateTime.Now
            };

            // Act
            SignificantDateDto dto = entity.ToDto();

            // Assert
            Assert.Equal(Guid.Empty, dto.EntityId);
        }

        [Fact]
        public void ToDtoShouldSetEntityTypeToPerson()
        {
            // Arrange
            SignificantDate entity = new()
            {
                Date = DateTime.Now
            };

            // Act
            SignificantDateDto dto = entity.ToDto();

            // Assert
            Assert.Equal(EntityTypes.Person, dto.EntityType);
        }

        [Fact]
        public void ToDtoShouldMapNullDescriptionAsNull()
        {
            // Arrange
            SignificantDate entity = new()
            {
                Description = null,
                Date = DateTime.Now
            };

            // Act
            SignificantDateDto dto = entity.ToDto();

            // Assert
            Assert.Null(dto.Description);
        }
    }
}
