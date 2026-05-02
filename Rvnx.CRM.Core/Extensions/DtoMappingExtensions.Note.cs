using Rvnx.CRM.Core.DTOs.Base;
using Rvnx.CRM.Core.Enumerations;
using Rvnx.CRM.Core.Models.Base;

namespace Rvnx.CRM.Core.Extensions;

public static partial class DtoMappingExtensions
{
    public static NoteDto ToDto(this Note entity)
    {
        return new NoteDto
        {
            Id = entity.Id,
            Title = entity.Title,
            Value = entity.Value,
            IsFavorite = entity.IsFavorite,
            CreatedDate = entity.CreatedDate,
            EntityId = entity.ContactId ?? Guid.Empty,
            EntityType = EntityType.Person
        };
    }

    public static Note ToEntity(this NoteFormDto dto)
    {
        return new Note
        {
            Id = Guid.NewGuid(),
            Title = dto.Title,
            Value = dto.Value,
            IsFavorite = dto.IsFavorite,
            ContactId = dto.EntityId
        };
    }

    public static void UpdateEntity(this Note entity, NoteFormDto dto)
    {
        entity.Title = dto.Title;
        entity.Value = dto.Value;
        entity.IsFavorite = dto.IsFavorite;
    }
}
