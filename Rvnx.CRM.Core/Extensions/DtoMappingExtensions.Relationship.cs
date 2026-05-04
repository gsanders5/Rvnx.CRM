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
            ContactId = entity.ContactId,
            RelatedContactId = entity.RelatedContactId,
            RelationshipTypeId = entity.RelationshipTypeId,
            RelationshipTypeName = typeName,
            RelationshipTypeOppositeName = oppositeName,
            RelationshipTypeCategory = def?.Category ?? "Uncategorized",
            RelatedContactName = entity.RelatedPerson?.FullName ?? "Unknown",
            ContactName = entity.Person?.FullName ?? "Unknown",
            IsContactPartial = (entity.Person as Contact)?.IsPartial == true,
            IsRelatedContactPartial = (entity.RelatedPerson as Contact)?.IsPartial == true,
            IsContactDeceased = entity.Person?.IsDeceased == true,
            IsRelatedContactDeceased = entity.RelatedPerson?.IsDeceased == true,
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
            ContactId = dto.ContactId,
            RelatedContactId = dto.RelatedContactId,
            RelationshipTypeId = dto.RelationshipTypeId,
            Description = dto.Description,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate
        };
    }

    public static void UpdateEntity(this Relationship entity, RelationshipFormDto dto)
    {
        entity.ContactId = dto.ContactId;
        entity.RelatedContactId = dto.RelatedContactId;
        entity.RelationshipTypeId = dto.RelationshipTypeId;
        entity.Description = dto.Description;
        entity.StartDate = dto.StartDate;
        entity.EndDate = dto.EndDate;
    }
}
