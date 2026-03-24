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

## 2026-03-09 - Duplicate repository helpers

**Learning:** Controllers inherited from `RepositoryController` and used duplicate logic for `GetEntityName` and `IsPartialContactAsync` which were already implemented in `EntityService` as `GetEntityNameAsync` and `IsPartialAsync`.
**Action:** Remove the duplicate helper methods in `RepositoryController` and switch controller actions to use the centralized methods in `IEntityService` to reduce redundancy.
## 2024-03-24 - Deduplication using HashSet.Add return value
**Learning:** Checking `HashSet.Contains()` immediately before `HashSet.Add()` is redundant because `Add()` already returns a boolean indicating whether the element was successfully added (not present) or false (already present). This applies across various areas in Rvnx.CRM batch operations.
**Action:** Use `if (set.Add(item))` directly to perform insertion and checking in a single operation, reducing verbosity and improving performance.

## 2024-03-24 - Renaming vague variables
**Learning:** In ASP.NET Core MVC, changing the variable name passed into `return View(model)` (e.g., from `data` to `dashboard`) does not change the model passed to the Razor view, making it a completely safe and localized refactor for clarity.
**Action:** When finding vague variables like `data`, `result`, `temp`, `obj`, `flag`, or `val` passed to Views, confidently rename them to reflect their actual type or purpose to improve readability without fear of breaking the view binding.
