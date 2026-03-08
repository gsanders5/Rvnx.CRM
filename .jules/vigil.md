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
## 2025-03-01 - [Test DashboardService]
**Learning:** `DashboardService` logic, including complex query chunking, priority queuing, and time interval logic (`GetTimeUntil`), lacked unit tests entirely. Furthermore, testing `ILogger` calls effectively when using `LoggerMessage.Define` requires careful mocking of both `IsEnabled` and the specific generic parameters passed to `Log`.
**Action:** When a service uses `LoggerMessage.Define`, ensure `_loggerMock.Setup(x => x.IsEnabled(LogLevel.Warning)).Returns(true);` is added to the arrange phase, and the `Verify` matches the specific `EventId` and generic type constraints used by the logger.
## 2026-03-03 - [RelationshipService Behavior and Dependencies] **Learning:** Relationship directional selection in `RelationshipService` uses a structured string format `"{Guid}_{Fwd|Rev}"`. The `_Rev` suffix explicitly triggers the swapping of `EntityId` and `RelatedEntityId` before the relationship is persisted. **Action:** When testing relationship operations involving directional options, always mock both the forward and reverse variations to ensure the entity swapping logic fires correctly.
## 2024-03-04 - [Testing EF Core Projections]
**Learning:** Testing `ListProjectedAsync` methods that project domain entities to anonymous types or strings requires proper in-memory database setup to ensure projection translation behaves as expected. The methods `ListProjectedAsync` are critical performance optimizations used extensively to avoid full entity materialization, but they lacked coverage.
**Action:** When adding new repository projection capabilities or similar performance optimizations, always ensure corresponding `ListProjectedAsync` variants are tested for basic projection mapping and ordering parameters.
## 2026-03-05 - [Testing Domain Logic Handled by Controllers vs Services]
**Learning:** Sometimes, domain logic (like creating a Self Contact) appears to be tested because there are tests for the related Controller endpoint (`SelfContactTests.cs` for `ContactsController`). However, if the Controller tests merely mock out the underlying service (`ISelfContactService`), the actual domain logic within the service (creating the user links, handling missing data, delegating to helpers) remains completely untested.
**Action:** Always investigate the `Core.Services` implementations even if `Tests.Services` has a similarly named test class. Do not assume a service is tested just because the controller that consumes it is tested. Create dedicated service tests (e.g., `SelfContactServiceTests.cs`) to explicitly verify the internal logic, especially when it delegates to unmockable internal helpers like `ContactUpdateHelper`.
## 2026-03-06 - [Bulk Loading Restitching Validation]
**Learning:** The `ContactReadService.GetIndexDataAsync` method relies heavily on bulk loading related entities (Profile Images, Labels, Birthdays) into secondary lists, which are then manually restitched into the `ContactDto` main list via `Dictionary` lookups (e.g., `attachmentMap.TryGetValue`). If not properly tested, a developer could easily introduce subtle bugs during refactoring, such as: failing to handle duplicate related entity records correctly in `GroupBy`, causing dictionary generation exceptions, or mis-mapping dictionary keys leading to mixed-up data in the final list view.
**Action:** Always ensure tests are in place for list projection methods that utilize bulk-loading and manual dictionary restitching. These tests must explicitly verify that (a) duplicate related entity keys are handled gracefully without exceptions (e.g., using `GroupBy().ToDictionary(g => g.Key, g => g.First())`), and (b) related entities are correctly correlated to the appropriate primary entity DTO without data leakage or mixing.

## 2024-05-24 - Testing Repository Extension Methods Indirectly
**Learning:** When writing unit tests for core services that use the `ListProjectedByChunkedContainsAsync` static extension method on `IRepository`, the extension method cannot be directly mocked using Moq. However, the extension internally relies on `IRepository.ListProjectedAsync`. Mocking the underlying interface method is necessary and sufficient to inject test data into the extension method's execution path.
**Action:** Always inspect the implementation of repository extension methods to identify the underlying `IRepository` interface methods they invoke, and mock those interface methods instead of attempting to mock the static extension itself.
