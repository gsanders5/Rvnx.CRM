Let's figure out what `IRelationshipService` needs to support:
1. `Task<List<SuggestedRelationshipDto>> GetSuggestedRelationshipsAsync(Guid entityId, Guid relatedEntityId, Guid relationshipTypeId);`
  - This should return contacts related to `relatedEntityId` via `relationshipTypeId` that are NOT already related to `entityId` via `relationshipTypeId`.
  - It should also return contacts related to `entityId` via `relationshipTypeId` that are NOT already related to `relatedEntityId`.
  - Wait, if Jack is Colleague of James. Jack links to Jill as Colleague.
    We should suggest Jill also links to James.
  - The UI says "Also add as Colleague to: [] James [] Carol".
    Who is being added? It's symmetric, so both. The UI can say "Also link Jack and James as Colleague".
    Wait, the UI: "Is Jack also a Colleague of James?" "Is Jack also a Colleague of Carol?"
    If Jack is linking Jill. Jill is already Colleague to James.
    We want to ask "Is Jack also a Colleague of James?". So the new relationship is `Jack <-> James`.
    If Jack is already Colleague to Bob. Jack links Jill as Colleague.
    We want to ask "Is Jill also a Colleague of Bob?". So the new relationship is `Jill <-> Bob`.

Let's just return a list of contacts with a descriptive message or just the contact name.
If Jack is on his own profile, linking Jill.
`EntityId` = Jack. `RelatedEntityId` = Jill.
Jill's colleagues: James. -> We should suggest `Jack <-> James`. (Entity `Jack` linked to `James`).
Jack's colleagues: Bob. -> We should suggest `Jill <-> Bob`. (RelatedEntity `Jill` linked to `Bob`).
We can return a DTO:
```csharp
public class SuggestedRelationshipDto
{
    public Guid SourceEntityId { get; set; } // e.g. Jack or Jill
    public string SourceEntityName { get; set; } = string.Empty;
    public Guid TargetEntityId { get; set; } // e.g. James or Bob
    public string TargetEntityName { get; set; } = string.Empty;
}
```
Wait, the user is submitting `Create` form for Jack -> Jill.
If the user selects a checkbox, we need to know exactly which relationship to create.
We just need to create `Relationship { EntityId = Source, RelatedEntityId = Target, RelationshipTypeId = ... }` for each checked item.

So in `RelationshipFormDto` (or `RelationshipFormViewModel` and `CreatePartialContactRelationshipDto`), we add:
```csharp
public List<string> SuggestedRelationships { get; set; } = [];
```
Where each string is a serialized payload `"{SourceEntityId}_{TargetEntityId}"`.
When creating, we iterate through `SuggestedRelationships`, and for each, we create a new `Relationship` of the same type.

Wait, should we suggest relationships for *asymmetric* types?
"Things like 'friend' could exclude this check though."
"This also should apply to siblings, cousins, basically most relationships."
If we stick to Symmetric ones: Sibling, Cousin, Step-Sibling, Half-Sibling, Colleague, Business Partner. (Exclude Friend, Best Friend, Acquaintance, Significant Other, Ex-Partner).
We can have a helper: `RelationshipTypeService.IsTransitiveSymmetric(Guid typeId)`
Or just an array of `TransitiveRelationshipTypeIds`.

Let's check `RelationshipTypeService.cs`.
It has:
```csharp
    public static readonly Guid Sibling = Guid.Parse("d4f1b8a9-3e2c-4b5d-9a6f-1c0e7d8b5a2f");
    public static readonly Guid Cousin = Guid.Parse("f7df8278-11f1-49a0-bfc9-960e1c40f3cd");
    public static readonly Guid StepSibling = Guid.Parse("985d3d47-1585-4788-867d-3062c2b9b78a");
    public static readonly Guid HalfSibling = Guid.Parse("3553b74c-88ba-4c1b-957c-867d6d80f319");
    public static readonly Guid Colleague = Guid.Parse("c2497ebc-1ec3-427b-a845-67d31f136f56");
    public static readonly Guid BusinessPartner = Guid.Parse("36a10f9d-dc1a-4803-ae52-26fd9b9c7ec5");
```
We can create a set of these IDs.

To build the `GetSuggestions` endpoint:
1. Validate type ID is in the transitive set.
2. If `entityId` is valid, find its existing relations of `typeId`.
3. If `relatedEntityId` is valid, find its existing relations of `typeId`.
4. We want to suggest:
   - For each of `target`'s relations `C`, suggest `source <-> C` (if not already existing).
   - For each of `source`'s relations `C`, suggest `target <-> C` (if not already existing).

Since it's symmetric, `source <-> C` and `C <-> source` are equivalent.
Let's formulate the response:
```json
[
  { "sourceId": "jack-id", "targetId": "james-id", "displayText": "Is Jack also a Colleague of James?" },
  { "sourceId": "jill-id", "targetId": "bob-id", "displayText": "Is Jill also a Colleague of Bob?" }
]
```

When checking if they already exist, we need to check if ANY relationship of type `typeId` exists between `sourceId` and `targetId`.

Let's look at `CreatePartialContactRelationshipDto`. It creates a new contact first. So the new contact has NO existing relationships!
So for `CreatePartial`, `target` has no relations. We only find `source`'s existing relations `C`, and suggest `newContact <-> C`.
Wait, how can the UI display suggestions if the new contact's name is typed in the form?
We can use JS to take the name from the input box!
But the new contact's ID is not known yet!
So instead of `sourceId` and `targetId`, the checkbox value can just be `targetId` (the existing contact).
Because one side is ALWAYS the "other" contact, and the other side is ALWAYS the newly linked contact pair.
Actually, if the user submits: `AlsoLinkTo: [ "james-id", "bob-id" ]`
For `Create`, we have `EntityId = Jack`, `RelatedEntityId = Jill`.
If the user checks James. James is a colleague of Jill. We should link Jack to James.
If the user checks Bob. Bob is a colleague of Jack. We should link Jill to Bob.
To know which is which on the server:
- Is James already a colleague of Jill? Yes. So link Jack to James.
- Is Bob already a colleague of Jack? Yes. So link Jill to Bob.
Wait, what if BOTH Jack and Jill are colleagues of Dave, then Dave wouldn't be suggested.
The server can just do the linking explicitly:
Checkbox value: `{JackId}_{JamesId}`.
Then the server just splits by `_` and creates `Relationship { EntityId = JackId, RelatedEntityId = JamesId }`.
For `CreatePartial`: we don't know the new contact's ID on the client.
So the client can just pass `targetId` (since the other ID is implicitly the new contact).
Let's use a simpler checkbox value:
`Checkbox value: {ExistingContactId}`.
If we use `{ExistingContactId}`, then on the server:
- For `Create`: The pair is `EntityId` and `RelatedEntityId`. For each checked `C`, we need to link `C` to whichever of `EntityId` or `RelatedEntityId` it is NOT currently linked to!
  - Check if `C` is linked to `EntityId`. If yes, link `C` to `RelatedEntityId`.
  - Check if `C` is linked to `RelatedEntityId`. If yes, link `C` to `EntityId`.
- For `CreatePartial`: The pair is `EntityId` and `NewContactId`. The checked `C` is linked to `EntityId`. So link `C` to `NewContactId`.
This is very elegant!
