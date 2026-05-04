using Rvnx.CRM.Core.DTOs.Contact;
using Rvnx.CRM.Core.Models.Contact;

namespace Rvnx.CRM.Core.Extensions;

public static partial class DtoMappingExtensions
{
    public static ContactTaskDto ToDto(this ContactTask entity)
    {
        return new ContactTaskDto
        {
            Id = entity.Id,
            Title = entity.Title,
            Description = entity.Description,
            DueDate = entity.DueDate,
            IsCompleted = entity.IsCompleted,
            CompletedDate = entity.CompletedDate,
            ContactId = entity.ContactId ?? Guid.Empty,
            CreatedDate = entity.CreatedDate,
            CreatedBy = entity.CreatedBy,
            LastChangedDate = entity.LastChangedDate,
            LastChangedBy = entity.LastChangedBy
        };
    }

    public static ContactTask ToEntity(this ContactTaskFormDto dto)
    {
        return new ContactTask
        {
            Id = Guid.NewGuid(),
            Title = dto.Title,
            Description = dto.Description,
            DueDate = dto.DueDate,
            IsCompleted = dto.IsCompleted,
            ContactId = dto.ContactId
        };
    }

    public static void UpdateEntity(this ContactTask entity, ContactTaskFormDto dto)
    {
        entity.Title = dto.Title;
        entity.Description = dto.Description;
        entity.DueDate = dto.DueDate;
        entity.IsCompleted = dto.IsCompleted;
    }
}
