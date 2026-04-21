using Rvnx.CRM.Core.Constants;
using Rvnx.CRM.Core.DTOs.Contact;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models.Business;
using Rvnx.CRM.Core.Models.Contact;

namespace Rvnx.CRM.Core.Services;

public class RelationshipSuggestionService(IRepository repository) : IRelationshipSuggestionService
{
    public async Task<bool> RelationshipDuplicateExistsAsync(
        Guid entityId, Guid relatedEntityId, Guid typeId, Guid? excludeId = null)
    {
        return await repository.CountAsync<Relationship>(r =>
            (excludeId == null || r.Id != excludeId) &&
            r.RelationshipTypeId == typeId &&
            ((r.EntityId == entityId && r.RelatedEntityId == relatedEntityId) ||
             (r.EntityId == relatedEntityId && r.RelatedEntityId == entityId))) > 0;
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

            Guid tIdQuery = relatedEntityId ?? Guid.Empty;
            List<Relationship> existingRels = await repository.ListAsNoTrackingAsync<Relationship>(r =>
                r.RelationshipTypeId == relationshipTypeId &&
                (r.EntityId == entityId || r.RelatedEntityId == entityId ||
                 r.EntityId == tIdQuery || r.RelatedEntityId == tIdQuery));
            HashSet<(Guid, Guid)> existingEdges = existingRels.Select(r => (r.EntityId, r.RelatedEntityId)).ToHashSet();

            foreach (Guid x in compEIds)
            {
                if (contactMap.TryGetValue(x, out Contact? xContact))
                {
                    string xName = $"{xContact.FirstName} {xContact.LastName}".Trim();
                    Guid tId = relatedEntityId ?? Guid.Empty;
                    AddSuggestion(x, tId, xName, relatedEntityName, isReverse, existingEdges);
                }
            }

            foreach (Guid y in compRIds)
            {
                if (contactMap.TryGetValue(y, out Contact? yContact))
                {
                    string yName = $"{yContact.FirstName} {yContact.LastName}".Trim();
                    AddSuggestion(entityId, y, entityName, yName, isReverse, existingEdges);
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

                List<Relationship> existingRels = await repository.ListAsNoTrackingAsync<Relationship>(r =>
                    r.RelationshipTypeId == relationshipTypeId &&
                    (r.EntityId == adultId || r.RelatedEntityId == adultId));
                HashSet<(Guid, Guid)> existingEdges = existingRels.Select(r => (r.EntityId, r.RelatedEntityId)).ToHashSet();

                foreach (Contact sibContact in sibContacts)
                {
                    string sibName = $"{sibContact.FirstName} {sibContact.LastName}".Trim();
                    AddSuggestion(adultId, sibContact.Id, adultName, sibName, false, existingEdges);
                }
            }
        }

        return suggestions;
    }
}
