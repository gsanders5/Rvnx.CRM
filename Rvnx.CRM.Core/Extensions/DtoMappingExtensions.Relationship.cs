using Rvnx.CRM.Core.DTOs.Contact;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Core.Services;

namespace Rvnx.CRM.Core.Extensions;

public static partial class DtoMappingExtensions
{
    public static RelationshipDto ToDto(this Relationship entity)
    {
        RelationshipTypeDefinition? def = RelationshipTypeService.GetById(entity.RelationshipTypeId);
        string typeName = def?.GetName((entity.Person as Contact)?.Gender) ?? entity.RelationshipTypeName;
        string oppositeName = def?.GetOppositeName((entity.RelatedPerson as Contact)?.Gender) ??
                              entity.RelationshipTypeOppositeName;

        return new RelationshipDto
        {
            Id = entity.Id,
            EntityId = entity.EntityId,
            EntityType = entity.EntityType,
            RelatedEntityId = entity.RelatedEntityId,
            RelationshipTypeId = entity.RelationshipTypeId,
            RelationshipTypeName = typeName,
            RelationshipTypeOppositeName = oppositeName,
            RelationshipTypeCategory = def?.Category ?? "Uncategorized",
            RelatedEntityName = entity.RelatedPerson?.FullName ?? "Unknown",
            EntityName = entity.Person?.FullName ?? "Unknown",
            IsEntityPartial = (entity.Person as Contact)?.IsPartial == true,
            IsRelatedEntityPartial = (entity.RelatedPerson as Contact)?.IsPartial == true,
            Description = entity.Description,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate
        };
    }

    public static Relationship ToEntity(this RelationshipFormDto dto)
    {
        return new Relationship
        {
            Id = Guid.NewGuid(),
            EntityId = dto.EntityId,
            RelatedEntityId = dto.RelatedEntityId,
            EntityType = dto.EntityType,
            RelationshipTypeId = dto.RelationshipTypeId,
            Description = dto.Description,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate
        };
    }

    public static void UpdateEntity(this Relationship entity, RelationshipFormDto dto)
    {
        entity.EntityId = dto.EntityId;
        entity.RelatedEntityId = dto.RelatedEntityId;
        entity.RelationshipTypeId = dto.RelationshipTypeId;
        entity.Description = dto.Description;
        entity.StartDate = dto.StartDate;
        entity.EndDate = dto.EndDate;
    }
}
