using Rvnx.CRM.Core.DTOs.Contact;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models.Contact;

namespace Rvnx.CRM.Core.Services;

public class RelationshipSuggestionService(IRepository repository) : IRelationshipSuggestionService
{
    public async Task<bool> RelationshipDuplicateExistsAsync(
        Guid contactId, Guid relatedContactId, Guid typeId, Guid? excludeId = null)
    {
        return await repository.CountAsync<Relationship>(r =>
            (excludeId == null || r.Id != excludeId) &&
            r.RelationshipTypeId == typeId &&
            ((r.ContactId == contactId && r.RelatedContactId == relatedContactId) ||
             (r.ContactId == relatedContactId && r.RelatedContactId == contactId))) > 0;
    }

    public async Task<List<SuggestedRelationshipDto>> GetSuggestedRelationshipsAsync(Guid contactId,
        Guid? relatedContactId, Guid relationshipTypeId, bool isReverse, string? partialContactName)
    {
        List<SuggestedRelationshipDto> suggestions = [];
        HashSet<string> seenPayloads = [];

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

        List<string> contactNames = await repository.ListProjectedAsync<Contact, string>(
            c => c.Id == contactId,
            c => (c.FirstName + " " + (c.LastName ?? "")).Trim());

        string? contactName = contactNames?.FirstOrDefault();
        if (contactName == null)
        {
            return suggestions;
        }

        string relatedContactDisplayName = partialContactName ?? string.Empty;
        if (relatedContactId.HasValue)
        {
            List<string> relatedContactNames = await repository.ListProjectedAsync<Contact, string>(
                c => c.Id == relatedContactId.Value,
                c => (c.FirstName + " " + (c.LastName ?? "")).Trim());

            string? relatedName = relatedContactNames?.FirstOrDefault();
            if (relatedName != null)
            {
                relatedContactDisplayName = relatedName;
            }
        }

        async Task<HashSet<Guid>> GetComponentAsync(Guid startId, Guid typeIdToSearch)
        {
            const int maxNodes = 50;
            HashSet<Guid> comp = [startId];
            HashSet<Guid> frontier = [startId];

            while (frontier.Count > 0 && comp.Count < maxNodes)
            {
                List<Relationship> edges = await repository.ListAsNoTrackingAsync<Relationship>(r =>
                    r.RelationshipTypeId == typeIdToSearch &&
                    (frontier.Contains(r.ContactId) || frontier.Contains(r.RelatedContactId)));

                HashSet<Guid> nextFrontier = [];
                foreach (Relationship edge in edges)
                {
                    if (comp.Count >= maxNodes)
                    {
                        break;
                    }
                    Guid neighbor = frontier.Contains(edge.ContactId) ? edge.RelatedContactId : edge.ContactId;
                    if (comp.Add(neighbor))
                    {
                        nextFrontier.Add(neighbor);
                    }
                }
                frontier = nextFrontier;
            }

            return comp;
        }

        void AddSuggestion(Guid sId, Guid tId, string sName, string tName, bool reverse, HashSet<(Guid, Guid)> existingEdges)
        {
            if (sId != Guid.Empty && tId != Guid.Empty)
            {
                if (existingEdges.Contains((sId, tId)) || existingEdges.Contains((tId, sId)))
                {
                    return;
                }
            }

            string payload = $"{sId}_{tId}_{reverse}";
            if (seenPayloads.Add(payload))
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
            HashSet<Guid> compE = await GetComponentAsync(contactId, relationshipTypeId);
            HashSet<Guid> compR = relatedContactId.HasValue
                ? await GetComponentAsync(relatedContactId.Value, relationshipTypeId)
                : [Guid.Empty];

            // Batch-load all contacts from both components in two queries instead of one per node
            HashSet<Guid> compEIds = compE.Where(id => id != contactId).ToHashSet();
            HashSet<Guid> compRIds = compR.Where(id => id != Guid.Empty && id != relatedContactId).ToHashSet();

            HashSet<Guid> allNeededIds = [.. compEIds, .. compRIds];
            List<Contact> batchContacts = allNeededIds.Count > 0
                ? await repository.ListAsNoTrackingAsync<Contact>(c => allNeededIds.Contains(c.Id))
                : [];

            Dictionary<Guid, Contact> contactMap = batchContacts.ToDictionary(c => c.Id);

            Guid tIdQuery = relatedContactId ?? Guid.Empty;
            List<Relationship> existingRels = await repository.ListAsNoTrackingAsync<Relationship>(r =>
                r.RelationshipTypeId == relationshipTypeId &&
                (r.ContactId == contactId || r.RelatedContactId == contactId ||
                 r.ContactId == tIdQuery || r.RelatedContactId == tIdQuery));

            HashSet<(Guid, Guid)> existingEdges = existingRels
                .Select(r => (r.ContactId, r.RelatedContactId))
                .ToHashSet();

            foreach (Guid x in compEIds)
            {
                if (contactMap.TryGetValue(x, out Contact? xContact))
                {
                    string xName = xContact.FullName;
                    Guid tId = relatedContactId ?? Guid.Empty;
                    AddSuggestion(x, tId, xName, relatedContactDisplayName, isReverse, existingEdges);
                }
            }

            foreach (Guid y in compRIds)
            {
                if (contactMap.TryGetValue(y, out Contact? yContact))
                {
                    string yName = yContact.FullName;
                    AddSuggestion(contactId, y, contactName, yName, isReverse, existingEdges);
                }
            }
        }
        else if (isFamilyAdultChild)
        {
            Guid adultId = isReverse ? (relatedContactId ?? Guid.Empty) : contactId;
            Guid childId = isReverse ? contactId : (relatedContactId ?? Guid.Empty);

            string adultName = isReverse ? relatedContactDisplayName : contactName;

            if (childId != Guid.Empty)
            {
                HashSet<Guid> childSiblings = await GetComponentAsync(childId, RelationshipTypeIds.Sibling);
                HashSet<Guid> siblingIds = childSiblings.Where(id => id != childId).ToHashSet();

                List<Contact> sibContacts = siblingIds.Count > 0
                    ? await repository.ListAsNoTrackingAsync<Contact>(c => siblingIds.Contains(c.Id))
                    : [];

                List<Relationship> existingRels = await repository.ListAsNoTrackingAsync<Relationship>(r =>
                    r.RelationshipTypeId == relationshipTypeId &&
                    (r.ContactId == adultId || r.RelatedContactId == adultId ||
                     r.ContactId == childId || r.RelatedContactId == childId));

                HashSet<(Guid, Guid)> existingEdges = existingRels
                    .Select(r => (r.ContactId, r.RelatedContactId))
                    .ToHashSet();

                foreach (Contact sibContact in sibContacts)
                {
                    string sibName = sibContact.FullName;
                    AddSuggestion(adultId, sibContact.Id, adultName, sibName, false, existingEdges);
                }
            }
        }

        return suggestions;
    }
}
