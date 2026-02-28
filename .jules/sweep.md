## 2024-05-22 - Polymorphic Entity Pattern

**Learning:** Most auxiliary entities (`Note`, `Reminder`, `Address`, etc.) inherit from `PolymorphicEntity`, which standardizes `EntityId` and `EntityType`. This allows for generic handling of related data, such as bulk deletion.
**Action:** When working with related entities, check if they inherit from `PolymorphicEntity` to use or create generic helper methods instead of repetitive code.

## 2026-02-19 - User Fetching Fallback Pattern

**Learning:** Fetching the current `User` entity requires checking both the internal ID (`GetByIdAsync`) and falling back to searching by external `SubjectId` (as a string).
**Action:** Centralise this dual-lookup logic into a single helper method (e.g., `GetUserAsync`) instead of repeating the two repository calls across controller actions.

## 2026-02-20 - Contact Validation Security

**Learning:** Relying solely on `IsPartialContactAsync` for validation allows non-existent contacts to bypass checks (as they are "not partial"). This can lead to orphaned records.
**Action:** Use `IsValidContactAsync` in `RepositoryController` which explicitly enforces both existence and non-partial status for contact-related operations.
## 2026-02-28 - [DateCalculationService - Avoid using try-catch for leap year flow control] **Learning:** The codebase contained a pattern of handling leap year edge cases (instantiating February 29th on non-leap years) by intentionally throwing and catching `ArgumentOutOfRangeException` and defaulting to February 28th. **Action:** Replace this exception-driven control flow with mathematical bounding using `Math.Min(expectedDay, DateTime.DaysInMonth(year, month))` to correctly and safely handle month-end day calculations.
