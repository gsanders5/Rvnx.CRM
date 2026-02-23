using Rvnx.CRM.Core.Constants;
using Rvnx.CRM.Core.DTOs.Common;
using Rvnx.CRM.Core.DTOs.Contact;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models.Business;
using Rvnx.CRM.Core.Models.Contact;

namespace Rvnx.CRM.Core.Services;

public class RelationshipService(IRepository repository) : IRelationshipService
{
    private readonly IRepository _repository = repository;

    public async Task<RelationshipOperationResult> CreateRelationshipAsync(Relationship relationship, string selectedRelationshipType)
    {
        var (typeId, isReverse, error) = ParseRelationshipSelection(selectedRelationshipType);
        if (error != null) return RelationshipOperationResult.Failure(error);

        relationship.RelationshipTypeId = typeId;

        if (isReverse) SwapRelationshipEntities(relationship);

        await _repository.AddAsync(relationship);
        await _repository.SaveChangesAsync();

        Guid redirectId = isReverse ? relationship.RelatedEntityId : relationship.EntityId;
        return RelationshipOperationResult.Ok(redirectId, relationship.EntityType);
    }

    public async Task<RelationshipOperationResult> UpdateRelationshipAsync(Guid id, Relationship updatedRelationship, string selectedRelationshipType)
    {
        var (typeId, isReverse, error) = ParseRelationshipSelection(selectedRelationshipType);
        if (error != null) return RelationshipOperationResult.Failure(error);

        Relationship? existingRelationship = await _repository.GetByIdAsync<Relationship>(id);
        if (existingRelationship == null)
            return RelationshipOperationResult.Failure("Relationship not found.");

        existingRelationship.EntityId = updatedRelationship.EntityId;
        existingRelationship.RelatedEntityId = updatedRelationship.RelatedEntityId;
        existingRelationship.EntityType = updatedRelationship.EntityType;
        existingRelationship.Description = updatedRelationship.Description;
        existingRelationship.StartDate = updatedRelationship.StartDate;
        existingRelationship.EndDate = updatedRelationship.EndDate;
        existingRelationship.RelationshipTypeId = typeId;

        if (isReverse) SwapRelationshipEntities(existingRelationship);

        await _repository.UpdateAsync(existingRelationship);
        await _repository.SaveChangesAsync();

        Guid redirectId = isReverse ? existingRelationship.RelatedEntityId : existingRelationship.EntityId;
        return RelationshipOperationResult.Ok(redirectId, existingRelationship.EntityType);
    }

    private static (Guid TypeId, bool IsReverse, string? Error) ParseRelationshipSelection(string selection)
    {
        if (string.IsNullOrEmpty(selection))
            return (Guid.Empty, false, "Relationship Type is required.");

        string[] parts = selection.Split('_');
        if (parts.Length != 2 || !Guid.TryParse(parts[0], out Guid typeId))
            return (Guid.Empty, false, "Invalid Relationship Type.");

        return (typeId, parts[1] == "Rev", null);
    }

    private static void SwapRelationshipEntities(Relationship relationship)
    {
        (relationship.EntityId, relationship.RelatedEntityId) = (relationship.RelatedEntityId, relationship.EntityId);
    }

    public async Task<List<SelectOptionDto>> GetRelatedEntityOptionsAsync(Guid entityId, string entityType, Guid? selectedId = null)
    {
        List<SelectOptionDto> options = new();

        if (entityType == EntityTypes.Person)
        {
            List<Contact> available = await _repository.ListAsNoTrackingAsync<Contact>(p => p.Id != entityId);
            available = available.OrderBy(p => p.FullName).ToList();
            options = available.Select(p => new SelectOptionDto
            {
                Value = p.Id.ToString(),
                Text = p.FullName,
                Selected = selectedId == p.Id
            }).ToList();
        }
        else if (entityType == EntityTypes.Company)
        {
            List<Employer> available = await _repository.ListAsNoTrackingAsync<Employer>(c => c.Id != entityId);
            available = available.OrderBy(c => c.CompanyName).ToList();
            options = available.Select(c => new SelectOptionDto
            {
                Value = c.Id.ToString(),
                Text = c.CompanyName,
                Selected = selectedId == c.Id
            }).ToList();
        }
        return options;
    }

    public List<SelectOptionDto> GetRelationshipTypeOptions(string entityType, string? selectedValue = null)
    {
        List<RelationshipTypeDefinition> types = RelationshipTypeService.GetByEntityType(entityType);
        types = types.OrderBy(t => t.Category).ThenBy(t => t.Name).ToList();

        List<SelectOptionDto> options = new();

        foreach (RelationshipTypeDefinition t in types)
        {
            string group = t.Category;

            string fwdText = t.IsSymmetric ? $"is {t.Name} of" : $"is {t.Name} of ({t.OppositeName})";
            options.Add(new SelectOptionDto
            {
                Value = $"{t.Id}_Fwd",
                Text = fwdText,
                Group = group,
                Selected = selectedValue == $"{t.Id}_Fwd"
            });

            if (!t.IsSymmetric)
            {
                string revText = $"is {t.OppositeName} of ({t.Name})";
                options.Add(new SelectOptionDto
                {
                    Value = $"{t.Id}_Rev",
                    Text = revText,
                    Group = group,
                    Selected = selectedValue == $"{t.Id}_Rev"
                });
            }
        }

        return options;
    }
}
