When we create a relationship A -> B of type T.
If we want to ask "Should A also have relationship T with C, D, E?" (where C, D, E are existing T-related contacts of B).
We can do this in the UI:
1. In `RelationshipsController.Create` and `CreatePartial` POST methods:
   - Check if there are other contacts related to B with the same relationship type (e.g. Sibling, Colleague).
   - If yes, we can redirect to a new action: `SuggestRelationships`.
   - Wait, `CreatePartial` might just be simpler to handle in the same `SuggestRelationships` view?
   - Wait, let's implement the AJAX approach instead! It is much smoother.

AJAX Approach:
1. Create a new `[HttpGet] GetSuggestions` endpoint in `RelationshipsController`
   - Parameters: `entityId` (Guid), `relatedEntityId` (Guid), `selectedRelationshipType` (string)
   - Parse `selectedRelationshipType` to get `typeId` and `isReverse`.
   - Find all `Relationship` entities where `(EntityId == relatedEntityId OR RelatedEntityId == relatedEntityId)` and `RelationshipTypeId == typeId`.
   - Filter out `entityId` itself.
   - For each matching relationship, return `{ id: "...", name: "..." }`.
   - But wait, relationships are stored differently:
     - E.g., Colleague is symmetric. A is Colleague of B. `EntityId = A, RelatedEntityId = B`.
     - So if we link Jack (entity) to Jill (relatedEntity) as Colleague (`isReverse` doesn't matter for symmetric).
     - We should find all relationships of Jill of type Colleague. This means `EntityId == Jill OR RelatedEntityId == Jill`.
     - Then the other side of the relationship is C (James, Carol).
     - Jack should also be Colleague to James, Carol.
     - So `GetSuggestions` returns a list of suggested entity IDs and names.

2. UI update:
   - Add a `<div id="suggestions-container" style="display:none;"></div>` to `Create.cshtml`.
   - In `RelationshipSelector` (JS), when `updatePreview` runs, if both `relatedEntityId` and `selectedRelationshipType` are set, call `fetch('/Relationships/GetSuggestions?...')`.
   - If results > 0, display a checkbox list: "Also add as <Type> to:"
     - [x] James
     - [x] Carol
   - These checkboxes will have `name="SuggestedEntityIds"` and value as the IDs.
   - Same for `CreatePartial.cshtml`? But `CreatePartial` creates a new contact first, so we don't know `relatedEntityId` until after creation!
   - Ah! If Jack creates a *new partial contact* Jill as a Sibling. Jill doesn't exist yet, so she doesn't have any existing siblings! So `GetSuggestions` is only relevant for linking to an *existing* contact.
   - Therefore, we only need suggestions on the "Link Existing Contact" tab (`Create` POST).
   - Actually, wait. "Is Jack also a Colleague of James?" This means we are adding Jack to Jill, who is already a colleague of James. So Jill already exists. This is indeed the "Link Existing Contact" scenario.

Wait, what if Jack creates a *new partial contact* "Jill" as a Colleague. Jill has no colleagues yet. Jack's existing colleagues are James and Carol. Should we ask "Is Jill also a Colleague of James and Carol?" Yes!
"If we go in the UI to link Jack to Jill, ask for if the relationship applies for every other related Colleague."
In this case, we are adding Jill to Jack. Jack already has colleagues James and Carol. So we should suggest Jack's existing colleagues for Jill!
Wait, the relationship is symmetric. A -> B is the same as B -> A.
If Jack adds Jill (new or existing), the relationship is between Jack and Jill.
If Jack already has James and Carol as colleagues, we can ask "Is Jill also a colleague of James and Carol?"
If Jill already has Bob as a colleague, we can ask "Is Jack also a colleague of Bob?"
So we need to find ALL colleagues of Jack AND all colleagues of Jill, and suggest them to be linked to the other person.
Actually, if I link Jack to Jill, and Jack has colleagues {James, Carol}, then Jill could be linked to {James, Carol}.
If Jill has colleagues {Bob}, then Jack could be linked to {Bob}.
So we are creating a bipartite matching? "Apply this relationship to other related contacts."
