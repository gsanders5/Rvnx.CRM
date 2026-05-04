using Rvnx.CRM.Core.DTOs.Base;
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
            ContactId = entity.ContactId ?? Guid.Empty,
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
            ContactId = dto.ContactId
        };
    }

    public static void UpdateEntity(this Note entity, NoteFormDto dto)
    {
        entity.Title = dto.Title;
        entity.Value = dto.Value;
        entity.IsFavorite = dto.IsFavorite;
    }
}
