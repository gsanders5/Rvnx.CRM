1. **Understand Goal**: When a new relationship is created, check if the related contact already has other relationships of the same type. If so, optionally prompt the user (or automatically prompt via UI) if the newly linked contact should also have this relationship applied to the other related contacts.
    * e.g., A is Colleague to B. C is added as Colleague to A. Ask "Is C also a Colleague of B?"
    * e.g., Siblings, Cousins, etc.

2. **Wait, let's rethink**: The requirement is "ask the user if the relationship applies to the contact as well".
    * "Is Jill, James, and Carol are already linked as Colleague, if we go in the UI to link Jack to Jill, ask for if the relationship applies for every other related Colleague."
    * This means we need an intermediate UI step after submitting `Create` or `CreatePartial` for relationships. Or maybe we can display the prompt right on the `Create` view before submission?
    * Displaying it on the `Create` view *before* submission is hard because the user selects the relationship type and related entity, and we would need AJAX to fetch "other colleagues" and show checkboxes.
    * An alternative is to do a post-create redirect to a "Bulk Apply" or "Suggested Relationships" view.
    * Another alternative is to use AJAX on the Create page. When `RelatedEntityId` and `SelectedRelationshipType` are set, fetch suggestions and show them.

Let's look at how to implement this AJAX fetch approach:
1. When `RelatedEntityId` is selected (and `SelectedRelationshipType` is known), call an endpoint `/Relationships/GetSuggestions?entityId={entityId}&relatedEntityId={relatedEntityId}&relationshipTypeId={typeId}`
2. The endpoint finds all entities related to `relatedEntityId` with the same `relationshipTypeId` (excluding `entityId`).
3. Return a partial view or JSON with these suggestions.
4. The user can check boxes for "Also apply to: James, Carol".
5. The `Create` and `CreatePartial` POST methods accept an array of `SuggestedEntityIds`.

Let's see the current `Create` POST signature.
