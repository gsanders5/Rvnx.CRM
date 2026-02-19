# Blueprint Journal

## 2024-05-22 - [Service Location and Dependencies]
**Learning:** Services implementing `Core` interfaces (e.g., `IUserSynchronizationService`) are found in `Web` when they handle web-specific concerns like `ClaimsPrincipal`, even if they also access `DbContext` directly. This creates a mix of web and data access concerns.
**Action:** When refactoring, prioritize extracting shared constants/contracts to `Core` to reduce coupling between these web-located services, rather than immediately moving the entire service which might be more disruptive.

## 2024-05-23 - [Test Construction Pattern]
**Learning:** Controller unit tests instantiate controllers directly via `new Controller(...)` rather than using a test fixture or DI container. This makes constructor injection changes ripple across all test files immediately.
**Action:** When adding dependencies to controllers, expect to update multiple test files manually. Consider introducing a test builder pattern if the pain becomes too high, but respect the current direct instantiation for consistency.
