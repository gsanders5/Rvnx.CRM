Let's refine the approach:

1. In `RelationshipTypeService`, add:
```csharp
public static readonly IReadOnlySet<Guid> TransitiveRelationshipTypeIds = new HashSet<Guid>
{
    RelationshipTypeIds.Sibling,
    RelationshipTypeIds.Cousin,
    RelationshipTypeIds.StepSibling,
    RelationshipTypeIds.HalfSibling,
    RelationshipTypeIds.Colleague,
    RelationshipTypeIds.BusinessPartner
};
```

2. Add a DTO:
```csharp
namespace Rvnx.CRM.Core.DTOs.Contact
{
    public class SuggestedRelationshipDto
    {
        public Guid ExistingContactId { get; set; }
        public string SourceName { get; set; } = string.Empty;
        public string TargetName { get; set; } = string.Empty;
        public string RelationshipName { get; set; } = string.Empty;
    }
}
```

3. In `RelationshipsController`, add a new `HttpGet` action:
```csharp
[HttpGet]
public async Task<IActionResult> GetSuggestions(Guid entityId, Guid? relatedEntityId, string relationshipType, string? partialContactName = null)
```
Inside `GetSuggestions`:
Parse `relationshipType`. Extract `typeId`.
If not in `TransitiveRelationshipTypeIds`, return `Json(new List<SuggestedRelationshipDto>())`.

Get `entityId`'s existing relations `E_Rels` of `typeId`.
If `relatedEntityId` is provided, get its existing relations `R_Rels` of `typeId`.
If `relatedEntityId` is provided, we check:
- For each `rel` in `E_Rels`, if `rel.OtherId` is not in `R_Rels` and `rel.OtherId != relatedEntityId`, suggest linking `rel.OtherId` to `relatedEntityId`.
- For each `rel` in `R_Rels`, if `rel.OtherId` is not in `E_Rels` and `rel.OtherId != entityId`, suggest linking `rel.OtherId` to `entityId`.

If `relatedEntityId` is null (Create Partial), we only have `E_Rels`:
- For each `rel` in `E_Rels`, suggest linking `rel.OtherId` to `partialContactName`.

Wait, how do we get `E_Rels`?
```csharp
var eRels = await _repository.ListProjectedAsync(
    (Relationship r) => r.RelationshipTypeId == typeId && (r.EntityId == entityId || r.RelatedEntityId == entityId),
    r => r.EntityId == entityId ? r.RelatedEntityId : r.EntityId
);
// We also need the names! So we project to an anonymous object with Id and Name.
// Wait, we can just use `IEntityService.GetEntityNameAsync(id, type)`. Or join.
// Or we can just get the relationships and then get names.
```

Let's do this logic in `IRelationshipService` instead.
```csharp
Task<List<SuggestedRelationshipDto>> GetSuggestedRelationshipsAsync(Guid entityId, Guid? relatedEntityId, Guid relationshipTypeId, string? partialContactName);
```

4. Update `RelationshipFormDto` and `CreatePartialContactRelationshipDto` to include:
```csharp
public List<Guid> SuggestedEntityIds { get; set; } = [];
```

5. In `RelationshipService.CreateRelationshipAsync` and `CreatePartialContactRelationshipAsync`:
Accept `List<Guid> suggestedEntityIds` as an argument or from the DTO.
Wait, `CreateRelationshipAsync` takes `Relationship relationship` which doesn't have `SuggestedEntityIds`.
I can pass `List<Guid> suggestedEntityIds` as a parameter to both `CreateRelationshipAsync` and `CreatePartialContactRelationshipAsync`.
Inside the service:
For each `suggestedId` in `suggestedEntityIds`:
We need to know who to link it to!
For `CreatePartial`: we link `suggestedId` to the newly created partial contact (`newContactId`).
For `Create`: we link `suggestedId` to either `entityId` or `relatedEntityId`.
How? Check if `suggestedId` is already linked to `entityId` via `typeId`.
If yes, link to `relatedEntityId`. Else link to `entityId`.
Wait, this check is easy:
```csharp
bool isLinkedToEntity = await repository.CountAsync<Relationship>(r => r.RelationshipTypeId == typeId && (r.EntityId == entityId || r.RelatedEntityId == entityId) && (r.EntityId == suggestedId || r.RelatedEntityId == suggestedId)) > 0;
Guid targetIdToLink = isLinkedToEntity ? relatedEntityId : entityId;
// Create new relationship between suggestedId and targetIdToLink.
```

6. Update the UI (`Create.cshtml`):
In `RelationshipSelector` (JS), when `updatePreview()` runs:
- Gather `entityId`, `relatedEntityId` (if existing tab), `selectedRelationshipType`.
- If partial tab, gather `partialContactName`.
- Call `/Relationships/GetSuggestions` via fetch.
- If results > 0, show a container with checkboxes:
```html
<div class="mt-3">
  <p class="fw-bold mb-2">Also apply this relationship to:</p>
  <!-- for each suggestion: -->
  <div class="form-check">
    <input class="form-check-input" type="checkbox" name="SuggestedEntityIds" value="<ExistingContactId>" id="sugg_<ExistingContactId>">
    <label class="form-check-label" for="sugg_<ExistingContactId>">
      Is <SourceName> also a <RelationshipName> of <TargetName>?
    </label>
  </div>
</div>
```

This covers everything!
