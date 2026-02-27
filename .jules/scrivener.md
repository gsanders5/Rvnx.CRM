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

## 2026-02-28 - Full Documentation Pass (Scrivener)

**Update**: `README.md` and `DESIGN.md` now reflect the .NET 8 / EF Core 9 stack and the shift to Explicit Foreign Keys.

**Feature**: Explicit Foreign Keys
**State**: Entities like `Note`, `Reminder`, `Attachment`, `ContactMethod`, etc., have moved away from generic polymorphic associations.
**Detail**: They now use typed, nullable FKs (e.g., `ContactId`) with Check Constraints (e.g., `CHK_Note_Owner`) ensuring that exactly one owner FK is populated (though currently `ContactId` is the primary one used). Future agents should check the `CRMDbContext.OnModelCreating` to see these constraints.

**Constraint**: Attachment Splitting
**State**: `Attachment` (metadata) and `AttachmentContent` (blob) are separate tables.
**Detail**: This prevents loading large blobs when only listing file names. When you need the file content, you must explicitly `Include(a => a.AttachmentContent)` or query the content table separately.

**Feature**: User Isolation
**State**: `GroupId` is the primary isolation key.
**Detail**: Global Query Filters on `BaseEntity` automatically filter by `GroupId`. `ICurrentUserService` resolves this ID from the `Users` table. Bypassing this filter (e.g. for admin tasks) requires `IgnoreQueryFilters()`.
