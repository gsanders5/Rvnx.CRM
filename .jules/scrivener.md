# Scrivener's Journal

## 2026-02-28 - Baseline Architecture Survey

**Feature**: Partial Contacts
**State**: Implemented as standard `Contact` entities with an `IsPartial` flag set to `true`.
**Detail**: Contrary to initial assumptions, partial contacts are not handled by nullable Foreign Keys on the `Relationship` entity. They are full entities in the `Contact` table. This allows them to have IDs, be promoted later, and potentially have other data attached, though they are filtered out of the main index view by default.

**Feature**: Data Stitching
**State**: `ContactReadService` manually stitches related data for index views.
**Detail**: To avoid N+1 issues and massive Cartesian products, the index view fetches Contacts first, then fetches Attachments (for profile images) and Labels in separate parallel/sequential queries using `Contains(Id)`. These are then stitched together in memory using Dictionaries. This is a critical pattern for performance in this application.

**Feature**: Polymorphic Relationships
**State**: `Relationship` entity uses `EntityId` and `EntityType`.
**Detail**: While `Relationship` is polymorphic, it currently only links `Person` to `Person` (via `RelatedEntityId`). The `RelationshipService` handles the logic for "Forward" and "Reverse" directions by swapping the IDs at persistence time based on the selected relationship type suffix (`_Fwd` or `_Rev`).

**Constraint**: `[NotMapped]` Collections
**State**: `Person` entity has `[NotMapped]` collections for `Relationships`.
**Detail**: EF Core does not automatically populate the `Relationships` collection on `Person`. This must be done manually by the service layer (specifically `ContactReadService`) when details are requested. Do not expect `Include(p => p.Relationships)` to work out of the box without custom configuration or service-level handling.
