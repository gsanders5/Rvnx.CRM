# Vigil's Journal

## 2026-02-19 - Initial Setup

**Learning:** Foundational journal for tracking critical testing insights in the Rvnx.CRM application.
**Action:** Let's focus on meaningful tests according to Vigil's philosophy.

## 2026-02-19 - Missing Failure Path Context Preservation Tests

**Learning:** Many POST actions in controllers (like `RelationshipsController.Create`) have logic that gracefully returns `View(dto)` when validation fails. However, if tests do not verify that reference data (`SelectLists`, `ViewData` identifiers) are re-populated on this failure path, future refactoring could easily drop the re-population code. This would cause the rendered View to silently crash in production with a `NullReferenceException` when it attempts to rebuild its dropdowns.
**Action:** Always add a failure-path validation test for any POST endpoint that returns a View populated with drop-downs. Assert that `ModelState.IsValid` is false AND importantly, that `ViewData["<YourSelectList>"]` is `NotNull` when the `ViewResult` is returned.

## 2026-02-20 - [Controller Testing Strategy]
**Learning:** Testing controllers inheriting from `RepositoryController` is more effective when using `CRMDbContext` with `InMemoryDatabase` and a real `Repository` instance, rather than mocking `IRepository`. This approach validates actual query logic (like global query filters and includes) while remaining fast. Mocking `IRepository` would require duplicating complex filter logic in setups, making tests brittle and less valuable.
**Action:** When testing standard CRUD controllers, prefer `InMemoryDatabase` + Real Repository over `Mock<IRepository>` to catch integration issues and validate query behavior.

## 2024-05-24 - [Date Calculation Ambiguity]
**Learning:** The `DateCalculationService` treats a `TimeSpan` of exactly 365 days as a "Calendar Year" rather than a strict duration of time. This means it uses `AddYears(1)` instead of adding 365 days. While likely intentional for business logic, this creates a hidden dependency on the magic number 365. A duration of 366 days (leap year length) or 730 days (2 years) behaves differently or consistently depending on the implementation details (modulo arithmetic).
**Action:** When testing date logic, always explicitly test "Yearly" frequencies against both standard and leap years to ensure the "Calendar Year" vs "Fixed Duration" behavior is pinned down. Future refactors must preserve this specific 365-day special casing.

## 2026-02-24 - [Testing LoggerMessage Delegates]
**Learning:** When testing classes that use `LoggerMessage.Define` (high-performance logging delegates), simply mocking `Log` is insufficient. These delegates explicitly check `IsEnabled(LogLevel)` before attempting to log. If the mock for `ILogger.IsEnabled` returns `false` (the default for `bool`), the `Log` method will never be called, causing `Verify` assertions to fail with confusing messages like "Expected 1 invocation, but was 0. Performed invocations: IsEnabled".
**Action:** When mocking `ILogger<T>` where `LoggerMessage` might be used, always setup `IsEnabled(It.IsAny<LogLevel>())` to return `true`.

## 2026-02-25 - [Bulk Loading Optimization Risks]
**Learning:** Performance optimizations in read services often involve fetching main entities first, collecting their IDs, and then performing secondary bulk queries for related data (like Profile Images or Labels) to avoid N+1 problems. This pattern manually restitches data in memory using Dictionaries. This logic is prone to bugs: forgetting to check for null keys, assuming a key exists in the dictionary, or mis-mapping IDs. Tests that only check single-entity retrieval often miss these "list view" bugs.
**Action:** When testing "Index" or "List" methods in services, explicitly test the mapping of these bulk-loaded related entities. Verify that the dictionary lookups handle missing keys gracefully and that the correct related entity is assigned to the correct parent.
