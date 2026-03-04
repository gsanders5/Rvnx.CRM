Let's look at `GetSuggestedRelationshipsAsync`:
It takes `(Guid entityId, Guid? relatedEntityId, Guid relationshipTypeId, string? partialContactName)`.
Wait, we also need to know `isReverse` for asymmetric relationships!
Because `FamilyAdultChildRelationshipTypeIds` applies `Adult -> Child`.
If `isReverse` is false, `entityId` is Adult, `relatedEntityId` is Child.
If `isReverse` is true, `entityId` is Child, `relatedEntityId` is Adult.

But wait, the URL gives us `relationshipType` (e.g. `typeId_Fwd` or `typeId_Rev`).
In `RelationshipsController.cs`, `GetSuggestions` receives `string relationshipType`.
We split it:
```csharp
            string[] parts = relationshipType.Split('_');
            if (parts.Length != 2 || !Guid.TryParse(parts[0], out Guid typeId))
            {
                return Json(new List<SuggestedRelationshipDto>());
            }
```
We need to pass `isReverse = parts[1] == "Rev"` to `GetSuggestedRelationshipsAsync`.
```csharp
Task<List<SuggestedRelationshipDto>> GetSuggestedRelationshipsAsync(Guid entityId, Guid? relatedEntityId, Guid relationshipTypeId, bool isReverse, string? partialContactName);
```

In `RelationshipService.GetSuggestedRelationshipsAsync`:

```csharp
public async Task<List<SuggestedRelationshipDto>> GetSuggestedRelationshipsAsync(Guid entityId, Guid? relatedEntityId, Guid relationshipTypeId, bool isReverse, string? partialContactName)
{
    List<SuggestedRelationshipDto> suggestions = [];

    bool isTransitive = RelationshipTypeService.TransitiveRelationshipTypeIds.Contains(relationshipTypeId);
    bool isFamilyAdultChild = RelationshipTypeService.FamilyAdultChildRelationshipTypeIds.Contains(relationshipTypeId);

    if (!isTransitive && !isFamilyAdultChild)
    {
        return suggestions;
    }

    RelationshipTypeDefinition? typeDef = RelationshipTypeService.GetById(relationshipTypeId);
    if (typeDef == null) return suggestions;

    // Names
    Contact? entity = await repository.GetByIdAsync<Contact>(entityId);
    if (entity == null) return suggestions;
    string entityName = $"{entity.FirstName} {entity.LastName}".Trim();

    string relatedEntityName = partialContactName ?? string.Empty;
    if (relatedEntityId.HasValue)
    {
        Contact? relatedEntity = await repository.GetByIdAsync<Contact>(relatedEntityId.Value);
        if (relatedEntity != null) relatedEntityName = $"{relatedEntity.FirstName} {relatedEntity.LastName}".Trim();
    }

    // Helper: Component BFS
    async Task<HashSet<Guid>> GetComponentAsync(Guid startId, Guid typeIdToSearch)
    {
        var comp = new HashSet<Guid>();
        var q = new Queue<Guid>();
        q.Enqueue(startId);
        comp.Add(startId);

        var allEdges = await repository.ListAsNoTrackingAsync<Relationship>(r => r.RelationshipTypeId == typeIdToSearch);
        var adj = new Dictionary<Guid, List<Guid>>();
        foreach (var edge in allEdges)
        {
            if (!adj.ContainsKey(edge.EntityId)) adj[edge.EntityId] = [];
            if (!adj.ContainsKey(edge.RelatedEntityId)) adj[edge.RelatedEntityId] = [];
            adj[edge.EntityId].Add(edge.RelatedEntityId);
            adj[edge.RelatedEntityId].Add(edge.EntityId);
        }

        while (q.Count > 0)
        {
            var curr = q.Dequeue();
            if (adj.TryGetValue(curr, out var nbrs))
            {
                foreach (var nbr in nbrs)
                {
                    if (comp.Add(nbr))
                    {
                        q.Enqueue(nbr);
                    }
                }
            }
        }
        return comp;
    }

    // Helper: Add suggestion
    async Task AddSuggestionAsync(Guid sId, Guid tId, string sName, string tName, bool reverse)
    {
        // Check if already linked
        bool exists = await repository.CountAsync<Relationship>(r => r.RelationshipTypeId == relationshipTypeId &&
            ((r.EntityId == sId && r.RelatedEntityId == tId) || (r.EntityId == tId && r.RelatedEntityId == sId))) > 0;

        if (!exists)
        {
            // payload tells the server what to do. format: SourceId_TargetId_IsReverse
            // The user action creates `EntityId = sId`, `RelatedEntityId = tId`.
            // Wait! The user action applies to Jack and Jill. The suggestion is `x` and `y`.
            // In the controller, we can just pass: "{EntityId}_{RelatedEntityId}_{IsReverse}"
            string payload = $"{sId}_{tId}_{reverse}";
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
        var compE = await GetComponentAsync(entityId, relationshipTypeId);
        var compR = relatedEntityId.HasValue ? await GetComponentAsync(relatedEntityId.Value, relationshipTypeId) : new HashSet<Guid>();
        if (!relatedEntityId.HasValue) compR.Add(Guid.Empty); // Placeholder for new contact

        // Suggest E's component links to R
        foreach (var x in compE)
        {
            if (x == entityId) continue;
            Contact? xContact = await repository.GetByIdAsync<Contact>(x);
            if (xContact != null)
            {
                string xName = $"{xContact.FirstName} {xContact.LastName}".Trim();
                // R is partial?
                Guid tId = relatedEntityId ?? Guid.Empty; // Guid.Empty means NEW contact!
                await AddSuggestionAsync(x, tId, xName, relatedEntityName, isReverse);
            }
        }

        // Suggest R's component links to E
        foreach (var y in compR)
        {
            if (y == relatedEntityId || y == Guid.Empty) continue;
            Contact? yContact = await repository.GetByIdAsync<Contact>(y);
            if (yContact != null)
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
        string childName = isReverse ? entityName : relatedEntityName;

        if (childId != Guid.Empty)
        {
            var childSiblings = await GetComponentAsync(childId, RelationshipTypeIds.Sibling);
            foreach (var sibling in childSiblings)
            {
                if (sibling == childId) continue;
                Contact? sibContact = await repository.GetByIdAsync<Contact>(sibling);
                if (sibContact != null)
                {
                    string sibName = $"{sibContact.FirstName} {sibContact.LastName}".Trim();
                    // Link adult to sibling
                    if (adultId == Guid.Empty)
                    {
                        // adult is the new contact
                        await AddSuggestionAsync(Guid.Empty, sibling, adultName, sibName, false);
                    }
                    else
                    {
                        await AddSuggestionAsync(adultId, sibling, adultName, sibName, false);
                    }
                }
            }
        }
    }

    return suggestions;
}
```

Wait, what if BOTH `adultId` and `childId` are `Guid.Empty`? One of them must be `entityId` which is never empty.
If `relatedEntityId` is null (Create Partial):
If `isReverse == false`: `adultId = entityId`, `childId = Guid.Empty`.
We cannot find siblings of `Guid.Empty`, so `childSiblings` is empty. No suggestions. Correct!
If `isReverse == true`: `adultId = Guid.Empty`, `childId = entityId`.
`childSiblings` are the siblings of `entityId`.
We suggest `Guid.Empty` (the new partial Adult) is also Adult to all `childSiblings`.
So we call `AddSuggestionAsync(Guid.Empty, sibling, adultName, sibName, false)`.
Wait, in `CreatePartialContactRelationshipAsync`:
We iterate `dto.SuggestedRelationships`.
Payload: `sId_tId_reverse`.
If `sId` is `Guid.Empty`, we replace it with `partialContact.Id`.
If `tId` is `Guid.Empty`, we replace it with `partialContact.Id`.
Then we just create `EntityId = sId`, `RelatedEntityId = tId`, `IsReverse = reverse`.
Wait, if `reverse` is true, we swap `EntityId` and `RelatedEntityId` before saving.
So we can just use the Payload literally!

Let's trace `Payload`:
`"{sId}_{tId}_{reverse}"`.
If `sId == 00000000-0000-0000-0000-000000000000`, `sId = newContactId`.
If `tId == 00000000-0000-0000-0000-000000000000`, `tId = newContactId`.
`bool isRev = bool.Parse(reverse)`.
Relationship `r = new Relationship { EntityId = sId, RelatedEntityId = tId, RelationshipTypeId = typeId, ... }`.
if `isRev`, swap `sId` and `tId`.
Save.

This is INCREDIBLY elegant and general.
It correctly handles BOTH "not stopping at one level deep" for Transitives (by finding the full Connected Component) AND "apply to Siblings" for Family relationships (by finding the Sibling Component of the child).

Let's refine `GetSuggestedRelationshipsAsync`:
