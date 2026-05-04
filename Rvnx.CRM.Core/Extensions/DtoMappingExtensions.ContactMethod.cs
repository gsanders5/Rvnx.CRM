using Rvnx.CRM.Core.DTOs.Contact;
using Rvnx.CRM.Core.Models.Contact;

namespace Rvnx.CRM.Core.Extensions;

public static partial class DtoMappingExtensions
{
    public static ContactMethodDto ToDto(this ContactMethod entity)
    {
        return new ContactMethodDto
        {
            Id = entity.Id,
            Type = entity.Type,
            Value = entity.Value,
            Label = entity.Label,
            ContactId = entity.ContactId ?? Guid.Empty,
            CreatedDate = entity.CreatedDate
        };
    }

    public static ContactMethod ToEntity(this ContactMethodFormDto dto)
    {
        return new ContactMethod
        {
            Id = Guid.NewGuid(),
            Type = dto.Type,
            Value = dto.Value,
            Label = dto.Label,
            ContactId = dto.ContactId
        };
    }

    public static void UpdateEntity(this ContactMethod entity, ContactMethodFormDto dto)
    {
        entity.Type = dto.Type;
        entity.Value = dto.Value;
        entity.Label = dto.Label;
    }
}
