## 2024-05-22 - Polymorphic Entity Pattern

**Learning:** Most auxiliary entities (`Note`, `Reminder`, `Address`, etc.) inherit from `PolymorphicEntity`, which standardizes `EntityId` and `EntityType`. This allows for generic handling of related data, such as bulk deletion.
**Action:** When working with related entities, check if they inherit from `PolymorphicEntity` to use or create generic helper methods instead of repetitive code.

## 2026-02-19 - User Fetching Fallback Pattern

**Learning:** Fetching the current `User` entity requires checking both the internal ID (`GetByIdAsync`) and falling back to searching by external `SubjectId` (as a string).
**Action:** Centralise this dual-lookup logic into a single helper method (e.g., `GetUserAsync`) instead of repeating the two repository calls across controller actions.

## 2026-03-01 - Consolidating Entity Creation and Updates

**Learning:** `ToEntity` methods often duplicated property assignments found in `UpdateEntity` methods.
**Action:** When implementing `ToEntity` for DTOs, initialize the entity with its ID and immutable fields, then call `UpdateEntity` to handle the rest, ensuring single responsibility for property mapping.
