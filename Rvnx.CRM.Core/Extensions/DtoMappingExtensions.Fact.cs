using Rvnx.CRM.Core.DTOs.Contact;
using Rvnx.CRM.Core.Enumerations;
using Rvnx.CRM.Core.Models.Contact;

namespace Rvnx.CRM.Core.Extensions;

public static partial class DtoMappingExtensions
{
    public static FactDto ToDto(this Fact entity)
    {
        return new FactDto
        {
            Id = entity.Id,
            Category = entity.Category,
            Value = entity.Value,
            EntityId = entity.ContactId ?? Guid.Empty,
            EntityType = EntityType.Person,
            CreatedDate = entity.CreatedDate
        };
    }

    public static Fact ToEntity(this FactFormDto dto)
    {
        return new Fact
        {
            Id = Guid.NewGuid(),
            Category = dto.Category,
            Value = dto.Value,
            ContactId = dto.EntityId
        };
    }

    public static void UpdateEntity(this Fact entity, FactFormDto dto)
    {
        entity.Category = dto.Category;
        entity.Value = dto.Value;
    }
}
