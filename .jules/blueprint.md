# Blueprint Journal

## 2024-05-22 - [Service Location and Dependencies]

**Learning:** Services implementing `Core` interfaces (e.g., `IUserSynchronizationService`) are found in `Web` when they handle web-specific concerns like `ClaimsPrincipal`, even if they also access `DbContext` directly. This creates a mix of web and data access concerns.
**Action:** When refactoring, prioritize extracting shared constants/contracts to `Core` to reduce coupling between these web-located services, rather than immediately moving the entire service which might be more disruptive.

## 2024-05-23 - [Test Construction Pattern]

**Learning:** Controller unit tests instantiate controllers directly via `new Controller(...)` rather than using a test fixture or DI container. This makes constructor injection changes ripple across all test files immediately.
**Action:** When adding dependencies to controllers, expect to update multiple test files manually. Consider introducing a test builder pattern if the pain becomes too high, but respect the current direct instantiation for consistency.

## 2026-02-20 - DashboardViewModel Types Used by Razor Views via Json.Serialize

**Learning:** The Razor views in `Views/Home/Index.cshtml` consume `GraphNodes` and `GraphLinks` via `@Json.Serialize(Model.GraphNodes)`. When moving DTO types between namespaces, the JSON property names must remain identical or the client-side JavaScript will break silently.
**Action:** When moving DTOs that are serialized to JSON for client-side consumption, verify property names match exactly. The Razor views don't use `@using` for the DTO types — they flow through the ViewModel properties.
