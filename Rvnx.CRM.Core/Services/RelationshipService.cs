using Rvnx.CRM.Core.Constants;
using Rvnx.CRM.Core.DTOs.Common;
using Rvnx.CRM.Core.DTOs.Contact;
using Rvnx.CRM.Core.Enumerations;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models;
using Rvnx.CRM.Core.Models.Business;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Core.Models.Dates;

namespace Rvnx.CRM.Core.Services;

public class RelationshipService(IRepository repository, IRelationshipSuggestionService suggestionService) : IRelationshipService
{
    public async Task<RelationshipOperationResult> CreateRelationshipAsync(Relationship relationship,
        string selectedRelationshipType, List<string>? suggestedEntityIds = null)
    {
        (Guid typeId, bool isReverse, string? error) = ParseRelationshipSelection(selectedRelationshipType);
        if (error != null)
        {
            return RelationshipOperationResult.Failure(error);
        }

        relationship.RelationshipTypeId = typeId;

        if (isReverse)
        {
            SwapRelationshipEntities(relationship);
        }

        if (await suggestionService.RelationshipDuplicateExistsAsync(relationship.EntityId, relationship.RelatedEntityId, typeId))
        {
            return RelationshipOperationResult.Failure(
                "This exact relationship already exists between these two contacts.");
        }

        await repository.AddAsync(relationship);

        await AddSuggestedRelationshipsAsync(relationship, typeId, suggestedEntityIds, partialContactId: null);

        await repository.SaveChangesAsync();

        Guid redirectId = isReverse ? relationship.RelatedEntityId : relationship.EntityId;
        return RelationshipOperationResult.Ok(redirectId, relationship.EntityType);
    }

    private async Task AddSuggestedRelationshipsAsync(
        Relationship primaryRelationship,
        Guid typeId,
        List<string>? suggestedEntityIds,
        Guid? partialContactId)
    {
        if (suggestedEntityIds == null || suggestedEntityIds.Count == 0)
        {
            return;
        }

        HashSet<Guid> allNodeIds = [];
        List<(Guid sId, Guid tId, bool reverse)> parsedSuggestions = [];

        foreach (string payload in suggestedEntityIds)
        {
            string[] parts = payload.Split('_');
            if (parts.Length == 3 && Guid.TryParse(parts[0], out Guid sId) &&
                Guid.TryParse(parts[1], out Guid tId) && bool.TryParse(parts[2], out bool reverse))
            {
                if (partialContactId.HasValue)
                {
                    if (sId == Guid.Empty)
                    {
                        sId = partialContactId.Value;
                    }

                    if (tId == Guid.Empty)
                    {
                        tId = partialContactId.Value;
                    }
                }

                parsedSuggestions.Add((sId, tId, reverse));
                allNodeIds.Add(sId);
                allNodeIds.Add(tId);
            }
        }

        if (parsedSuggestions.Count == 0)
        {
            return;
        }

        List<Relationship> existingRels = await repository.ListAsNoTrackingAsync<Relationship>(r =>
            r.RelationshipTypeId == typeId &&
            allNodeIds.Contains(r.EntityId) &&
            allNodeIds.Contains(r.RelatedEntityId));

        // Include the primary relationship being added in this transaction to avoid duplicates
        HashSet<(Guid, Guid)> existingEdges =
        [
            (primaryRelationship.EntityId, primaryRelationship.RelatedEntityId),
            (primaryRelationship.RelatedEntityId, primaryRelationship.EntityId),
        ];

        foreach (Relationship r in existingRels)
        {
            existingEdges.Add((r.EntityId, r.RelatedEntityId));
            existingEdges.Add((r.RelatedEntityId, r.EntityId));
        }

        foreach ((Guid sId, Guid tId, bool reverse) in parsedSuggestions)
        {
            Relationship newRel = new()
            {
                EntityId = sId,
                RelatedEntityId = tId,
                EntityType = primaryRelationship.EntityType,
                RelationshipTypeId = typeId,
                Description = "Automatically added from suggested relationship."
            };

            if (reverse)
            {
                SwapRelationshipEntities(newRel);
            }

            if (existingEdges.Add((newRel.EntityId, newRel.RelatedEntityId)))
            {
                await repository.AddAsync(newRel);
                existingEdges.Add((newRel.RelatedEntityId, newRel.EntityId));
            }
        }
    }

    public async Task<RelationshipOperationResult> UpdateRelationshipAsync(Guid id,
        Relationship updatedRelationship,
        string selectedRelationshipType)
    {
        (Guid typeId, bool isReverse, string? error) = ParseRelationshipSelection(selectedRelationshipType);
        if (error != null)
        {
            return RelationshipOperationResult.Failure(error);
        }

        Relationship? existingRelationship = await repository.GetByIdAsync<Relationship>(id);
        if (existingRelationship == null)
        {
            return RelationshipOperationResult.Failure("Relationship not found.");
        }

        existingRelationship.EntityId = updatedRelationship.EntityId;
        existingRelationship.RelatedEntityId = updatedRelationship.RelatedEntityId;
        existingRelationship.EntityType = updatedRelationship.EntityType;
        existingRelationship.Description = updatedRelationship.Description;
        existingRelationship.StartDate = updatedRelationship.StartDate;
        existingRelationship.EndDate = updatedRelationship.EndDate;
        existingRelationship.RelationshipTypeId = typeId;

        if (isReverse)
        {
            SwapRelationshipEntities(existingRelationship);
        }

        if (await suggestionService.RelationshipDuplicateExistsAsync(
                existingRelationship.EntityId, existingRelationship.RelatedEntityId, typeId, excludeId: id))
        {
            return RelationshipOperationResult.Failure(
                "This exact relationship already exists between these two contacts.");
        }

        await repository.UpdateAsync(existingRelationship);
        await repository.SaveChangesAsync();

        Guid redirectId = isReverse ? existingRelationship.RelatedEntityId : existingRelationship.EntityId;
        return RelationshipOperationResult.Ok(redirectId, existingRelationship.EntityType);
    }

    private static (Guid TypeId, bool IsReverse, string? Error) ParseRelationshipSelection(string selection)
    {
        if (string.IsNullOrEmpty(selection))
        {
            return (Guid.Empty, false, "Relationship Type is required.");
        }

        string[] parts = selection.Split('_');
        if (parts.Length != 2 || !Guid.TryParse(parts[0], out Guid typeId))
        {
            return (Guid.Empty, false, "Invalid Relationship Type.");
        }

        return (typeId, parts[1] == "Rev", null);
    }

    private static void SwapRelationshipEntities(Relationship relationship)
    {
        (relationship.EntityId, relationship.RelatedEntityId) =
            (relationship.RelatedEntityId, relationship.EntityId);
    }

    public async Task<List<SelectOptionDto>> GetRelatedEntityOptionsAsync(Guid entityId, EntityType entityType,
        Guid? selectedId = null)
    {
        return entityType switch
        {
            EntityType.Person => await GetPersonOptionsAsync(entityId, selectedId),
            EntityType.Company => await GetCompanyOptionsAsync(entityId, selectedId),
            _ => []
        };
    }

    private async Task<List<SelectOptionDto>> GetPersonOptionsAsync(Guid entityId, Guid? selectedId)
    {
        return await repository.ListProjectedAsync<Contact, SelectOptionDto, string>(
            p => p.Id != entityId,
            p => new SelectOptionDto
            {
                Value = p.Id.ToString(),
                Text = p.IsPartial
                    ? p.FirstName + " " + (p.LastName ?? "") + " (partial contact)"
                    : p.FirstName + " " + (p.LastName ?? ""),
                Selected = selectedId == p.Id
            },
            p => p.FirstName + " " + (p.LastName ?? ""));
    }

    private async Task<List<SelectOptionDto>> GetCompanyOptionsAsync(Guid entityId, Guid? selectedId)
    {
        return await repository.ListProjectedAsync<Employer, SelectOptionDto, string>(
            c => c.Id != entityId,
            c => new SelectOptionDto
            {
                Value = c.Id.ToString(),
                Text = c.CompanyName,
                Selected = selectedId == c.Id
            },
            c => c.CompanyName);
    }

    public List<SelectOptionDto> GetRelationshipTypeOptions(EntityType entityType, string? selectedValue = null)
    {
        List<RelationshipTypeDefinition> types = RelationshipTypeService.GetByEntityType(entityType);
        types = [.. types.OrderBy(t => t.Category).ThenBy(t => t.Name)];

        return types.SelectMany(t => MapToSelectOptions(t, selectedValue)).ToList();
    }

    private static IEnumerable<SelectOptionDto> MapToSelectOptions(RelationshipTypeDefinition t,
        string? selectedValue)
    {
        yield return CreateForwardOption(t, selectedValue);

        if (!t.IsSymmetric)
        {
            yield return CreateReverseOption(t, selectedValue);
        }
    }

    private static SelectOptionDto CreateForwardOption(RelationshipTypeDefinition t, string? selectedValue)
    {
        string fwdText = t.IsSymmetric ? $"is {t.Name} of" : $"is {t.Name} of ({t.OppositeName})";
        return new SelectOptionDto
        {
            Value = $"{t.Id}_Fwd",
            Text = fwdText,
            Group = t.Category,
            Selected = selectedValue == $"{t.Id}_Fwd"
        };
    }

    private static SelectOptionDto CreateReverseOption(RelationshipTypeDefinition t, string? selectedValue)
    {
        return new SelectOptionDto
        {
            Value = $"{t.Id}_Rev",
            Text = $"is {t.OppositeName} of ({t.Name})",
            Group = t.Category,
            Selected = selectedValue == $"{t.Id}_Rev"
        };
    }

    public List<RelationshipTypeDefinition> GetRelationshipTypes(EntityType entityType)
    {
        return
            [.. RelationshipTypeService.GetByEntityType(entityType).OrderBy(t => t.Category).ThenBy(t => t.Name)];
    }

    public async Task<RelationshipOperationResult> CreatePartialContactRelationshipAsync(Guid parentEntityId,
        string selectedRelationshipType, CreatePartialContactRelationshipDto dto)
    {
        (Guid typeId, bool isReverse, string? error) = ParseRelationshipSelection(selectedRelationshipType);
        if (error != null)
        {
            return RelationshipOperationResult.Failure(error);
        }

        Contact partialContact = new()
        {
            Id = Guid.NewGuid(),
            IsPartial = true,
            FirstName = dto.PartialContactFirstName,
            LastName = dto.PartialContactLastName
        };

        await repository.AddAsync(partialContact);

        if (dto.Birthday.HasValue)
        {
            SignificantDate bday = new()
            {
                Id = Guid.NewGuid(),
                ContactId = partialContact.Id,
                Title = SignificantDateTitles.Birthday,
                EventDate = DateOnly.FromDateTime(dto.Birthday.Value),
                Description = "Birthday",
                RecurrenceType = Enumerations.RecurrenceType.Annual,
                IsActive = true
            };
            await repository.AddAsync(bday);
        }

        Relationship relationship = new()
        {
            Id = Guid.NewGuid(),
            EntityId = parentEntityId,
            RelatedEntityId = partialContact.Id,
            EntityType = EntityType.Person,
            RelationshipTypeId = typeId,
            Description = dto.Description
        };

        if (isReverse)
        {
            SwapRelationshipEntities(relationship);
        }

        await repository.AddAsync(relationship);

        await AddSuggestedRelationshipsAsync(relationship, typeId, dto.SuggestedRelationships, partialContact.Id);

        await repository.SaveChangesAsync();

        return RelationshipOperationResult.Ok(parentEntityId, EntityType.Person);
    }

    public async Task<RelationshipOperationResult> PromotePartialContactAsync(Guid contactId)
    {
        Contact? contact = await repository.GetByIdAsync<Contact>(contactId);
        if (contact == null)
        {
            return RelationshipOperationResult.Failure("Contact not found.");
        }

        if (!contact.IsPartial)
        {
            return RelationshipOperationResult.Failure("Contact is not a partial contact.");
        }

        contact.IsPartial = false;
        await repository.UpdateAsync(contact);
        await repository.SaveChangesAsync();

        return RelationshipOperationResult.Ok(contact.Id, EntityType.Person);
    }

    public async Task<Relationship?> GetRelationshipForEditAsync(Guid id)
    {
        return await repository.GetByIdAsync<Relationship>(id);
    }

    public async Task<Relationship?> GetRelationshipForDeleteAsync(Guid id)
    {
        Relationship? relationship = await repository.GetByIdAsync<Relationship>(id);
        if (relationship == null)
        {
            return null;
        }

        Guid p1Id = relationship.EntityId;
        Guid p2Id = relationship.RelatedEntityId;
        List<Contact> contacts = await repository.ListAsync<Contact>(c => c.Id == p1Id || c.Id == p2Id);

        relationship.Person = contacts.FirstOrDefault(c => c.Id == p1Id);
        relationship.RelatedPerson = contacts.FirstOrDefault(c => c.Id == p2Id);

        return relationship;
    }

    public async Task<OperationResult> DeleteRelationshipAsync(Guid id)
    {
        List<(Guid EntityId, EntityType EntityType)> relationshipInfos = await repository.ListProjectedAsync<Relationship, (Guid EntityId, EntityType EntityType)>(
            r => r.Id == id,
            r => new ValueTuple<Guid, EntityType>(r.EntityId, r.EntityType));

        if (relationshipInfos.Count > 0)
        {
            (Guid entityId, EntityType entityType) = relationshipInfos[0];
            await repository.DeleteAsync<Relationship>(r => r.Id == id);
            await repository.SaveChangesAsync();
            return OperationResult.Ok(entityId, entityType);
        }

        return OperationResult.Failure("Relationship not found.");
    }
}
