using Rvnx.CRM.Core.DTOs.Contact;
using Rvnx.CRM.Core.Models.Activity;

namespace Rvnx.CRM.Core.Extensions;

public static partial class DtoMappingExtensions
{
    public static ActivityDto ToDto(this Activity entity)
    {
        List<Guid> contactIds = entity.ActivityContacts?.Select(ac => ac.ContactId).ToList() ?? [];
        return new ActivityDto
        {
            Id = entity.Id,
            Title = entity.Title,
            Description = entity.Description,
            ActivityDate = entity.ActivityDate,
            ActivityType = entity.ActivityType,
            Location = entity.Location,
            EntityId = contactIds.FirstOrDefault(),
            ContactIds = contactIds,
            CreatedDate = entity.CreatedDate,
            CreatedBy = entity.CreatedBy,
            LastChangedDate = entity.LastChangedDate,
            LastChangedBy = entity.LastChangedBy
        };
    }

    public static Activity ToEntity(this ActivityFormDto dto)
    {
        return new Activity
        {
            Id = Guid.NewGuid(),
            Title = dto.Title,
            Description = dto.Description,
            ActivityDate = dto.ActivityDate,
            ActivityType = dto.ActivityType,
            Location = dto.Location
        };
    }

    public static void UpdateEntity(this Activity entity, ActivityFormDto dto)
    {
        entity.Title = dto.Title;
        entity.Description = dto.Description;
        entity.ActivityDate = dto.ActivityDate;
        entity.ActivityType = dto.ActivityType;
        entity.Location = dto.Location;
    }
}
