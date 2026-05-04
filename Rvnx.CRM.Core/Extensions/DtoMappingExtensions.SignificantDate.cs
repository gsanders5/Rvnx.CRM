using Rvnx.CRM.Core.DTOs.Dates;
using Rvnx.CRM.Core.Enumerations;
using Rvnx.CRM.Core.Models.Dates;

namespace Rvnx.CRM.Core.Extensions;

public static partial class DtoMappingExtensions
{
    public static SignificantDateDto ToDto(this SignificantDate entity)
    {
        return new SignificantDateDto
        {
            Id = entity.Id,
            Title = entity.Title ?? string.Empty,
            EventDate = entity.EventDate,
            Description = entity.Description,
            ContactId = entity.ContactId ?? Guid.Empty,
            RecurrenceType = entity.RecurrenceType,
            CustomIntervalDays = entity.CustomIntervalDays,
            IsActive = entity.IsActive,
            NextOccurrence = entity.GetNextOccurrence(),
            ReminderOffsets = entity.ReminderOffsets?.Select(ro => new ReminderOffsetDto
            {
                Id = ro.Id,
                DaysBeforeEvent = ro.DaysBeforeEvent,
                IsActive = ro.IsActive,
                ScheduledFor =
                    Services.DateCalculationService.GetScheduledForDate(entity, ro,
                        DateOnly.FromDateTime(DateTime.Today))
            }).ToList() ?? []
        };
    }
}
