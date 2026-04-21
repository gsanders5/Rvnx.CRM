using Rvnx.CRM.Core.DTOs.Contact;
using Rvnx.CRM.Core.Models.Contact;

namespace Rvnx.CRM.Core.Extensions;

public static partial class DtoMappingExtensions
{
    public static PetDto ToDto(this Pet entity)
    {
        List<Guid> contactIds = entity.PetContacts?.Select(pc => pc.ContactId).ToList() ?? [];
        return new PetDto
        {
            Id = entity.Id,
            Name = entity.Name,
            Species = entity.Species,
            Breed = entity.Breed,
            Birthday = entity.Birthday,
            Notes = entity.Notes,
            ContactIds = contactIds,
            EntityId = contactIds.FirstOrDefault()
        };
    }

    public static Pet ToEntity(this PetFormDto dto)
    {
        return new Pet
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            Species = dto.Species,
            Breed = dto.Breed,
            Birthday = dto.Birthday,
            Notes = dto.Notes
        };
    }

    public static void UpdateEntity(this Pet entity, PetFormDto dto)
    {
        entity.Name = dto.Name;
        entity.Species = dto.Species;
        entity.Breed = dto.Breed;
        entity.Birthday = dto.Birthday;
        entity.Notes = dto.Notes;
    }
}
