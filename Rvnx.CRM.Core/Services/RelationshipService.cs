using Rvnx.CRM.Core.Constants;
using Rvnx.CRM.Core.DTOs.Common;
using Rvnx.CRM.Core.DTOs.Contact;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models;
using Rvnx.CRM.Core.Models.Business;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Core.Models.Dates;

namespace Rvnx.CRM.Core.Services;

public class RelationshipService(IRepository repository) : IRelationshipService
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

        if (await RelationshipDuplicateExistsAsync(relationship.EntityId, relationship.RelatedEntityId, typeId))
        {
            return RelationshipOperationResult.Failure(
                "This exact relationship already exists between these two contacts.");
        }

        await repository.AddAsync(relationship);

        if (suggestedEntityIds != null && suggestedEntityIds.Count > 0)
        {
            HashSet<Guid> allNodeIds = [];
            List<(Guid sId, Guid tId, bool reverse)> parsedSuggestions = [];

            foreach (string payload in suggestedEntityIds)
            {
                string[] parts = payload.Split('_');
                if (parts.Length == 3 && Guid.TryParse(parts[0], out Guid sId) &&
                    Guid.TryParse(parts[1], out Guid tId) && bool.TryParse(parts[2], out bool reverse))
                {
                    parsedSuggestions.Add((sId, tId, reverse));
                    allNodeIds.Add(sId);
                    allNodeIds.Add(tId);
                }
            }

            if (parsedSuggestions.Count > 0)
            {
                List<Relationship> existingRels = await repository.ListAsNoTrackingAsync<Relationship>(r =>
                    r.RelationshipTypeId == typeId &&
                    allNodeIds.Contains(r.EntityId) &&
                    allNodeIds.Contains(r.RelatedEntityId));

                HashSet<(Guid, Guid)> existingEdges = [];
                // Include the primary relationship being added in this transaction to avoid duplicates
                existingEdges.Add((relationship.EntityId, relationship.RelatedEntityId));
                existingEdges.Add((relationship.RelatedEntityId, relationship.EntityId));

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
                        EntityType = relationship.EntityType,
                        RelationshipTypeId = typeId,
                        Description = "Automatically added from suggested relationship."
                    };

                    if (reverse)
                    {
                        SwapRelationshipEntities(newRel);
                    }

                    if (!existingEdges.Contains((newRel.EntityId, newRel.RelatedEntityId)))
                    {
                        await repository.AddAsync(newRel);
                        existingEdges.Add((newRel.EntityId, newRel.RelatedEntityId));
                        existingEdges.Add((newRel.RelatedEntityId, newRel.EntityId));
                    }
                }
            }
        }

        await repository.SaveChangesAsync();

        Guid redirectId = isReverse ? relationship.RelatedEntityId : relationship.EntityId;
        return RelationshipOperationResult.Ok(redirectId, relationship.EntityType);
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

        if (await RelationshipDuplicateExistsAsync(
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

    /// <summary>
    /// Returns true if a relationship of the given type already exists between the two entities
    /// in either direction. Pass <paramref name="excludeId"/> when editing to ignore the
    /// relationship being updated.
    /// </summary>
    private async Task<bool> RelationshipDuplicateExistsAsync(
        Guid entityId, Guid relatedEntityId, Guid typeId, Guid? excludeId = null)
    {
        return await repository.CountAsync<Relationship>(r =>
            (excludeId == null || r.Id != excludeId) &&
            r.RelationshipTypeId == typeId &&
            ((r.EntityId == entityId && r.RelatedEntityId == relatedEntityId) ||
             (r.EntityId == relatedEntityId && r.RelatedEntityId == entityId))) > 0;
    }

    public async Task<List<SelectOptionDto>> GetRelatedEntityOptionsAsync(Guid entityId, string entityType,
        Guid? selectedId = null)
    {
        return entityType switch
        {
            EntityTypes.Person => await GetPersonOptionsAsync(entityId, selectedId),
            EntityTypes.Company => await GetCompanyOptionsAsync(entityId, selectedId),
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

    public List<SelectOptionDto> GetRelationshipTypeOptions(string entityType, string? selectedValue = null)
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

    public List<RelationshipTypeDefinition> GetRelationshipTypes(string entityType)
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
            EntityType = EntityTypes.Person,
            RelationshipTypeId = typeId,
            Description = dto.Description
        };

        if (isReverse)
        {
            SwapRelationshipEntities(relationship);
        }

        await repository.AddAsync(relationship);

        if (dto.SuggestedRelationships != null && dto.SuggestedRelationships.Count > 0)
        {
            HashSet<Guid> allNodeIds = [];
            List<(Guid sId, Guid tId, bool reverse)> parsedSuggestions = [];

            foreach (string payload in dto.SuggestedRelationships)
            {
                string[] parts = payload.Split('_');
                if (parts.Length == 3 && Guid.TryParse(parts[0], out Guid sId) &&
                    Guid.TryParse(parts[1], out Guid tId) && bool.TryParse(parts[2], out bool reverse))
                {
                    if (sId == Guid.Empty)
                    {
                        sId = partialContact.Id;
                    }

                    if (tId == Guid.Empty)
                    {
                        tId = partialContact.Id;
                    }

                    parsedSuggestions.Add((sId, tId, reverse));
                    allNodeIds.Add(sId);
                    allNodeIds.Add(tId);
                }
            }

            if (parsedSuggestions.Count > 0)
            {
                List<Relationship> existingRels = await repository.ListAsNoTrackingAsync<Relationship>(r =>
                    r.RelationshipTypeId == typeId &&
                    allNodeIds.Contains(r.EntityId) &&
                    allNodeIds.Contains(r.RelatedEntityId));

                HashSet<(Guid, Guid)> existingEdges = [];
                // Include the primary relationship being added in this transaction to avoid duplicates
                existingEdges.Add((relationship.EntityId, relationship.RelatedEntityId));
                existingEdges.Add((relationship.RelatedEntityId, relationship.EntityId));

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
                        EntityType = EntityTypes.Person,
                        RelationshipTypeId = typeId,
                        Description = "Automatically added from suggested relationship."
                    };

                    if (reverse)
                    {
                        SwapRelationshipEntities(newRel);
                    }

                    if (!existingEdges.Contains((newRel.EntityId, newRel.RelatedEntityId)))
                    {
                        await repository.AddAsync(newRel);
                        existingEdges.Add((newRel.EntityId, newRel.RelatedEntityId));
                        existingEdges.Add((newRel.RelatedEntityId, newRel.EntityId));
                    }
                }
            }
        }

        await repository.SaveChangesAsync();

        return RelationshipOperationResult.Ok(parentEntityId, EntityTypes.Person);
    }

    public async Task<List<SuggestedRelationshipDto>> GetSuggestedRelationshipsAsync(Guid entityId,
        Guid? relatedEntityId, Guid relationshipTypeId, bool isReverse, string? partialContactName)
    {
        List<SuggestedRelationshipDto> suggestions = [];

        bool isTransitive = RelationshipTypeService.TransitiveRelationshipTypeIds.Contains(relationshipTypeId);
        bool isFamilyAdultChild =
            RelationshipTypeService.FamilyAdultChildRelationshipTypeIds.Contains(relationshipTypeId);

        if (!isTransitive && !isFamilyAdultChild)
        {
            return suggestions;
        }

        RelationshipTypeDefinition? typeDef = RelationshipTypeService.GetById(relationshipTypeId);
        if (typeDef == null)
        {
            return suggestions;
        }

        Contact? entity = await repository.GetByIdAsync<Contact>(entityId);
        if (entity == null)
        {
            return suggestions;
        }

        string entityName = $"{entity.FirstName} {entity.LastName}".Trim();

        string relatedEntityName = partialContactName ?? string.Empty;
        if (relatedEntityId.HasValue)
        {
            Contact? relatedEntity = await repository.GetByIdAsync<Contact>(relatedEntityId.Value);
            if (relatedEntity != null)
            {
                relatedEntityName = $"{relatedEntity.FirstName} {relatedEntity.LastName}".Trim();
            }
        }

        async Task<HashSet<Guid>> GetComponentAsync(Guid startId, Guid typeIdToSearch)
        {
            HashSet<Guid> comp = [];
            Queue<Guid> q = new();
            q.Enqueue(startId);
            comp.Add(startId);

            int maxNodes = 50;

            while (q.Count > 0 && comp.Count < maxNodes)
            {
                Guid curr = q.Dequeue();
                List<Relationship> edges = await repository.ListAsNoTrackingAsync<Relationship>(r =>
                    r.RelationshipTypeId == typeIdToSearch && (r.EntityId == curr || r.RelatedEntityId == curr));

                foreach (Relationship edge in edges)
                {
                    Guid nbr = edge.EntityId == curr ? edge.RelatedEntityId : edge.EntityId;
                    if (comp.Add(nbr))
                    {
                        q.Enqueue(nbr);
                    }
                }
            }

            return comp;
        }

        async Task AddSuggestionAsync(Guid sId, Guid tId, string sName, string tName, bool reverse)
        {
            if (sId != Guid.Empty && tId != Guid.Empty)
            {
                bool exists = await repository.CountAsync<Relationship>(r =>
                    r.RelationshipTypeId == relationshipTypeId &&
                    ((r.EntityId == sId && r.RelatedEntityId == tId) ||
                     (r.EntityId == tId && r.RelatedEntityId == sId))) > 0;
                if (exists)
                {
                    return;
                }
            }

            string payload = $"{sId}_{tId}_{reverse}";
            // Avoid duplicates in suggestions (if multiple paths lead to same edge)
            if (!suggestions.Any(s => s.Payload == payload))
            {
                suggestions.Add(new SuggestedRelationshipDto
                {
                    Payload = payload,
                    SourceName = sName,
                    TargetName = tName,
                    RelationshipName = reverse ? typeDef.OppositeName : typeDef.Name
                });
            }
        }

        if (isTransitive)
        {
            HashSet<Guid> compE = await GetComponentAsync(entityId, relationshipTypeId);
            HashSet<Guid> compR = relatedEntityId.HasValue
                ? await GetComponentAsync(relatedEntityId.Value, relationshipTypeId)
                : [Guid.Empty];

            // Batch-load all contacts from both components in two queries instead of one per node
            HashSet<Guid> compEIds = compE.Where(id => id != entityId).ToHashSet();
            HashSet<Guid> compRIds = compR.Where(id => id != Guid.Empty && id != relatedEntityId).ToHashSet();

            HashSet<Guid> allNeededIds = [.. compEIds.Concat(compRIds)];
            List<Contact> batchContacts = allNeededIds.Count > 0
                ? await repository.ListAsNoTrackingAsync<Contact>(c => allNeededIds.Contains(c.Id))
                : [];
            Dictionary<Guid, Contact> contactMap = batchContacts.ToDictionary(c => c.Id);

            foreach (Guid x in compEIds)
            {
                if (contactMap.TryGetValue(x, out Contact? xContact))
                {
                    string xName = $"{xContact.FirstName} {xContact.LastName}".Trim();
                    Guid tId = relatedEntityId ?? Guid.Empty;
                    await AddSuggestionAsync(x, tId, xName, relatedEntityName, isReverse);
                }
            }

            foreach (Guid y in compRIds)
            {
                if (contactMap.TryGetValue(y, out Contact? yContact))
                {
                    string yName = $"{yContact.FirstName} {yContact.LastName}".Trim();
                    await AddSuggestionAsync(entityId, y, entityName, yName, isReverse);
                }
            }
        }
        else if (isFamilyAdultChild)
        {
            Guid adultId = isReverse ? (relatedEntityId ?? Guid.Empty) : entityId;
            Guid childId = isReverse ? entityId : (relatedEntityId ?? Guid.Empty);

            string adultName = isReverse ? relatedEntityName : entityName;

            if (childId != Guid.Empty)
            {
                HashSet<Guid> childSiblings = await GetComponentAsync(childId, RelationshipTypeIds.Sibling);
                HashSet<Guid> siblingIds = childSiblings.Where(id => id != childId).ToHashSet();

                List<Contact> sibContacts = siblingIds.Count > 0
                    ? await repository.ListAsNoTrackingAsync<Contact>(c => siblingIds.Contains(c.Id))
                    : [];

                foreach (Contact sibContact in sibContacts)
                {
                    string sibName = $"{sibContact.FirstName} {sibContact.LastName}".Trim();
                    await AddSuggestionAsync(adultId, sibContact.Id, adultName, sibName, false);
                }
            }
        }

        return suggestions;
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

        return RelationshipOperationResult.Ok(contact.Id, EntityTypes.Person);
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
        Relationship? relationship = await repository.GetByIdAsync<Relationship>(id);
        if (relationship != null)
        {
            Guid entityId = relationship.EntityId;
            string entityType = relationship.EntityType;
            await repository.DeleteAsync<Relationship>(id);
            await repository.SaveChangesAsync();
            return OperationResult.Ok(entityId, entityType);
        }

        return OperationResult.Failure("Relationship not found.");
    }
}