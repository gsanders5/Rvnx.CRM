using Rvnx.CRM.Core.DTOs.Base;
using Rvnx.CRM.Core.Enumerations;
using Rvnx.CRM.Core.Models.Base;

namespace Rvnx.CRM.Core.Extensions;

public static partial class DtoMappingExtensions
{
    public static AttachmentDto ToDto(this Attachment entity)
    {
        return new AttachmentDto
        {
            Id = entity.Id,
            FileName = entity.FileName ?? string.Empty,
            ContentType = entity.ContentType,
            AttachmentType = entity.AttachmentType,
            EntityId = entity.ContactId ?? Guid.Empty,
            EntityType = EntityType.Person
        };
    }
}
