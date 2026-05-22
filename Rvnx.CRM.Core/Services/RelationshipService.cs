using Rvnx.CRM.Core.Constants;
using Rvnx.CRM.Core.DTOs.Common;
using Rvnx.CRM.Core.DTOs.Contact;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Core.Models.Dates;

namespace Rvnx.CRM.Core.Services;

public class RelationshipService(IRepository repository, IRelationshipSuggestionService suggestionService) : IRelationshipService
{
    private const string ForwardSuffix = "_Fwd";
    private const string ReverseSuffix = "_Rev";


    public async Task<RelationshipOperationResult> CreateRelationshipAsync(Relationship relationship,
        string selectedRelationshipType, List<string>? suggestedContactIds = null)
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

        if (await suggestionService.RelationshipDuplicateExistsAsync(relationship.ContactId, relationship.RelatedContactId, typeId))
        {
            return RelationshipOperationResult.Failure(
                "This exact relationship already exists between these two contacts.");
        }

        await repository.AddAsync(relationship);

        await AddSuggestedRelationshipsAsync(relationship, typeId, suggestedContactIds, partialContactId: null);

        await repository.SaveChangesAsync();

        Guid redirectId = isReverse ? relationship.RelatedContactId : relationship.ContactId;
        return RelationshipOperationResult.Ok(redirectId);
    }

    private async Task AddSuggestedRelationshipsAsync(
        Relationship primaryRelationship,
        Guid typeId,
        List<string>? suggestedContactIds,
        Guid? partialContactId)
    {
        if (suggestedContactIds == null || suggestedContactIds.Count == 0)
        {
            return;
        }

        HashSet<Guid> allNodeIds = [];
        List<(Guid sId, Guid tId, bool reverse)> parsedSuggestions = [];

        foreach (string payload in suggestedContactIds)
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
            allNodeIds.Contains(r.ContactId) &&
            allNodeIds.Contains(r.RelatedContactId));

        // Include the primary relationship being added in this transaction to avoid duplicates
        HashSet<(Guid, Guid)> existingEdges =
        [
            (primaryRelationship.ContactId, primaryRelationship.RelatedContactId),
            (primaryRelationship.RelatedContactId, primaryRelationship.ContactId),
        ];

        foreach (Relationship r in existingRels)
        {
            existingEdges.Add((r.ContactId, r.RelatedContactId));
            existingEdges.Add((r.RelatedContactId, r.ContactId));
        }

        foreach (var (sId, tId, reverse) in parsedSuggestions)
        {
            Relationship newRel = new()
            {
                ContactId = sId,
                RelatedContactId = tId,
                RelationshipTypeId = typeId,
                Description = "Automatically added from suggested relationship."
            };

            if (reverse)
            {
                SwapRelationshipEntities(newRel);
            }

            if (existingEdges.Add((newRel.ContactId, newRel.RelatedContactId)))
            {
                await repository.AddAsync(newRel);
                existingEdges.Add((newRel.RelatedContactId, newRel.ContactId));
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

        existingRelationship.ContactId = updatedRelationship.ContactId;
        existingRelationship.RelatedContactId = updatedRelationship.RelatedContactId;
        existingRelationship.Description = updatedRelationship.Description;
        existingRelationship.StartDate = updatedRelationship.StartDate;
        existingRelationship.EndDate = updatedRelationship.EndDate;
        existingRelationship.RelationshipTypeId = typeId;

        if (isReverse)
        {
            SwapRelationshipEntities(existingRelationship);
        }

        if (await suggestionService.RelationshipDuplicateExistsAsync(
                existingRelationship.ContactId, existingRelationship.RelatedContactId, typeId, excludeId: id))
        {
            return RelationshipOperationResult.Failure(
                "This exact relationship already exists between these two contacts.");
        }

        await repository.UpdateAsync(existingRelationship);
        await repository.SaveChangesAsync();

        Guid redirectId = isReverse ? existingRelationship.RelatedContactId : existingRelationship.ContactId;
        return RelationshipOperationResult.Ok(redirectId);
    }

    private static (Guid TypeId, bool IsReverse, string? Error) ParseRelationshipSelection(string selection)
    {
        if (string.IsNullOrEmpty(selection))
        {
            return (Guid.Empty, false, "Relationship Type is required.");
        }

        bool isReverse = selection.EndsWith(ReverseSuffix, StringComparison.Ordinal);
        string suffix = isReverse ? ReverseSuffix : ForwardSuffix;
        if (!selection.EndsWith(suffix, StringComparison.Ordinal) ||
            !Guid.TryParse(selection[..^suffix.Length], out Guid typeId))
        {
            return (Guid.Empty, false, "Invalid Relationship Type.");
        }

        return (typeId, isReverse, null);
    }

    private static void SwapRelationshipEntities(Relationship relationship)
    {
        (relationship.ContactId, relationship.RelatedContactId) =
            (relationship.RelatedContactId, relationship.ContactId);
    }

    public async Task<List<SelectOptionDto>> GetRelatedContactOptionsAsync(Guid contactId, Guid? selectedId = null)
    {
        return await repository.ListProjectedAsync<Contact, SelectOptionDto, string>(
            p => p.Id != contactId,
            p => new SelectOptionDto
            {
                Value = p.Id.ToString(),
                Text = p.FirstName + " " + (p.LastName ?? "") +
                    (p.IsPartial && p.IsDeceased ? " (Partial Contact, Deceased)"
                     : p.IsPartial ? " (Partial Contact)"
                     : p.IsDeceased ? " (Deceased)"
                     : ""),
                Selected = selectedId == p.Id
            },
            p => p.FirstName + " " + (p.LastName ?? ""));
    }

    public List<SelectOptionDto> GetRelationshipTypeOptions(string? selectedValue = null)
    {
        return [.. RelationshipTypeService.GetAll().SelectMany(t => MapToSelectOptions(t, selectedValue))];
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
            Value = $"{t.Id}{ForwardSuffix}",
            Text = fwdText,
            Group = t.Category,
            Selected = selectedValue == $"{t.Id}{ForwardSuffix}"
        };
    }

    private static SelectOptionDto CreateReverseOption(RelationshipTypeDefinition t, string? selectedValue)
    {
        return new SelectOptionDto
        {
            Value = $"{t.Id}{ReverseSuffix}",
            Text = $"is {t.OppositeName} of ({t.Name})",
            Group = t.Category,
            Selected = selectedValue == $"{t.Id}{ReverseSuffix}"
        };
    }

    public List<RelationshipTypeDefinition> GetRelationshipTypes()
    {
        return [.. RelationshipTypeService.GetAll()];
    }

    public async Task<RelationshipOperationResult> CreatePartialContactRelationshipAsync(Guid parentContactId,
        string selectedRelationshipType, CreatePartialContactRelationshipDto dto)
    {
        (Guid typeId, bool isReverse, string? error) = ParseRelationshipSelection(selectedRelationshipType);
        if (error != null)
        {
            return RelationshipOperationResult.Failure(error);
        }

        if (await repository.GetByIdAsync<Contact>(parentContactId) == null)
        {
            return RelationshipOperationResult.Failure("Parent contact not found.");
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
            ContactId = parentContactId,
            RelatedContactId = partialContact.Id,
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

        return RelationshipOperationResult.Ok(parentContactId);
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

        return RelationshipOperationResult.Ok(contact.Id);
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

        Guid p1Id = relationship.ContactId;
        Guid p2Id = relationship.RelatedContactId;
        List<Contact> contacts = await repository.ListAsync<Contact>(c => c.Id == p1Id || c.Id == p2Id);

        relationship.Person = contacts.FirstOrDefault(c => c.Id == p1Id);
        relationship.RelatedPerson = contacts.FirstOrDefault(c => c.Id == p2Id);

        return relationship;
    }

    public async Task<OperationResult> DeleteRelationshipAsync(Guid id)
    {
        List<Guid> contactIds = await repository.ListProjectedAsync<Relationship, Guid>(
            r => r.Id == id,
            r => r.ContactId);

        if (contactIds.Count > 0)
        {
            await repository.DeleteAsync<Relationship>(r => r.Id == id);
            await repository.SaveChangesAsync();
            return OperationResult.Ok(contactIds[0]);
        }

        return OperationResult.Failure("Relationship not found.");
    }
}
