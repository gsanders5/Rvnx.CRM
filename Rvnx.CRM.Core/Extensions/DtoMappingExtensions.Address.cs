using Rvnx.CRM.Core.DTOs.Contact;
using Rvnx.CRM.Core.Models.Contact;

namespace Rvnx.CRM.Core.Extensions;

public static partial class DtoMappingExtensions
{
    public static AddressDto ToDto(this Address entity)
    {
        return new AddressDto
        {
            Id = entity.Id,
            Line1 = entity.Line1,
            Line2 = entity.Line2,
            City = entity.City,
            State = entity.State,
            Zip = entity.Zip,
            Country = entity.Country,
            AddressType = entity.AddressType,
            EntityId = entity.ContactId ?? Guid.Empty,
            CreatedDate = entity.CreatedDate,
            CreatedBy = entity.CreatedBy,
            LastChangedDate = entity.LastChangedDate,
            LastChangedBy = entity.LastChangedBy
        };
    }

    public static Address ToEntity(this AddressFormDto dto)
    {
        return new Address
        {
            Id = Guid.NewGuid(),
            Line1 = dto.Line1,
            Line2 = dto.Line2,
            City = dto.City,
            State = dto.State,
            Zip = dto.Zip,
            Country = dto.Country,
            AddressType = dto.AddressType,
            ContactId = dto.EntityId
        };
    }

    public static void UpdateEntity(this Address entity, AddressFormDto dto)
    {
        entity.Line1 = dto.Line1;
        entity.Line2 = dto.Line2;
        entity.City = dto.City;
        entity.State = dto.State;
        entity.Zip = dto.Zip;
        entity.Country = dto.Country;
        entity.AddressType = dto.AddressType;
    }
}
