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
            SignificantDate entity = new()
            {
                Id = Guid.NewGuid(),
                Title = "Anniversary",
                EventDate = new DateOnly(2023, 10, 27),
                Description = "A special day",
                ContactId = Guid.NewGuid(),
                RecurrenceType = Core.Enumerations.RecurrenceType.Annual,
                CustomIntervalDays = null,
                IsActive = true
            };

            SignificantDateDto dto = entity.ToDto();

            Assert.Equal(entity.Id, dto.Id);
            Assert.Equal(entity.Title, dto.Title);
            Assert.Equal(entity.EventDate, dto.EventDate);
            Assert.Equal(entity.Description, dto.Description);
            Assert.Equal(entity.ContactId, dto.EntityId);
            Assert.Equal(EntityTypes.Person, dto.EntityType);
            Assert.Equal(entity.RecurrenceType, dto.RecurrenceType);
            Assert.Equal(entity.CustomIntervalDays, dto.CustomIntervalDays);
            Assert.Equal(entity.IsActive, dto.IsActive);
        }

        [Fact]
        public void ToDtoShouldMapNullTitleToEmptyString()
        {
            SignificantDate entity = new()
            {
                Title = null,
                EventDate = DateOnly.FromDateTime(DateTime.Now)
            };

            SignificantDateDto dto = entity.ToDto();

            Assert.Equal(string.Empty, dto.Title);
        }

        [Fact]
        public void ToDtoShouldMapNullContactIdToEmptyGuid()
        {
            SignificantDate entity = new()
            {
                ContactId = null,
                EventDate = DateOnly.FromDateTime(DateTime.Now)
            };

            SignificantDateDto dto = entity.ToDto();

            Assert.Equal(Guid.Empty, dto.EntityId);
        }

        [Fact]
        public void ToDtoShouldSetEntityTypeToPerson()
        {
            SignificantDate entity = new()
            {
                EventDate = DateOnly.FromDateTime(DateTime.Now)
            };

            SignificantDateDto dto = entity.ToDto();

            Assert.Equal(EntityTypes.Person, dto.EntityType);
        }

        [Fact]
        public void ToDtoShouldMapNullDescriptionAsNull()
        {
            SignificantDate entity = new()
            {
                Description = null,
                EventDate = DateOnly.FromDateTime(DateTime.Now)
            };

            SignificantDateDto dto = entity.ToDto();

            Assert.Null(dto.Description);
        }
    }
}
