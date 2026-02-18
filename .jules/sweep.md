## 2024-05-22 - Polymorphic Entity Pattern
**Learning:** Most auxiliary entities (`Note`, `Reminder`, `Address`, etc.) inherit from `PolymorphicEntity`, which standardizes `EntityId` and `EntityType`. This allows for generic handling of related data, such as bulk deletion.
**Action:** When working with related entities, check if they inherit from `PolymorphicEntity` to use or create generic helper methods instead of repetitive code.
