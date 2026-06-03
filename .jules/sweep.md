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

## 2024-03-24 - Deduplication using HashSet.Add return value
**Learning:** Checking `HashSet.Contains()` immediately before `HashSet.Add()` is redundant because `Add()` already returns a boolean indicating whether the element was successfully added (not present) or false (already present). This applies across various areas in Rvnx.CRM batch operations.
**Action:** Use `if (set.Add(item))` directly to perform insertion and checking in a single operation, reducing verbosity and improving performance.

## 2024-03-24 - Renaming vague variables
**Learning:** In ASP.NET Core MVC, changing the variable name passed into `return View(model)` (e.g., from `data` to `dashboard`) does not change the model passed to the Razor view, making it a completely safe and localized refactor for clarity.
**Action:** When finding vague variables like `data`, `result`, `temp`, `obj`, `flag`, or `val` passed to Views, confidently rename them to reflect their actual type or purpose to improve readability without fear of breaking the view binding.
## 2026-03-09 - Deconstruct tuples in foreach loops
**Learning:** Using explicit tuple types in a `foreach` loop (e.g. `foreach ((Guid EntityId, Guid RelatedEntityId) relationship in relationships)`) is verbose and less readable than using tuple deconstruction.
**Action:** Use tuple deconstruction with `var` (e.g. `foreach (var (entityId, relatedEntityId) in relationships)`) when iterating over collections of tuples to improve clarity and reduce noise.
## 2024-05-17 - Prefer Target-Typed new()
**Learning:** The repository prefers modern C# features like target-typed `new()` over verbose `var` declarations for object instantiation, specifically within unit tests. This maintains consistency with C# 12 features used elsewhere.
**Action:** When creating new objects or refactoring existing `var` assignments where the type is explicitly known on the right side, replace it with the target-typed `new()` syntax on the left side.

## 2024-05-23 - C# Tuple Deconstruction Code Review Error
**Learning:** The code review tool flagged tuple deconstruction (`foreach ((Guid id, Guid relatedId) in list)`) as a compilation error when iterating over a `List<(Guid, Guid)>`. This was a hallucination, as `dotnet build` and `dotnet test` passed without issue.
**Action:** Always trust successful `dotnet build` and `dotnet test` results over the reviewer claims of basic C# compilation failures.
## 2024-05-18 - Simplify foreach loop tuple deconstruction
**Learning:** Explicit tuple type declarations in foreach loops (e.g. `foreach ((Guid id, string name) in list)`) add unnecessary visual noise and verbosity. C# allows deconstructing using `var` directly (e.g. `foreach (var (id, name) in list)`), improving code clarity.
**Action:** Use `var` for tuple deconstruction in foreach loops to improve readability while maintaining strongly-typed behavior.
