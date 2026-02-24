# Blueprint Journal

## 2024-05-22 - [Service Location and Dependencies]

**Learning:** Services implementing `Core` interfaces (e.g., `IUserSynchronizationService`) are found in `Web` when they handle web-specific concerns like `ClaimsPrincipal`, even if they also access `DbContext` directly. This creates a mix of web and data access concerns.
**Action:** When refactoring, prioritize extracting shared constants/contracts to `Core` to reduce coupling between these web-located services, rather than immediately moving the entire service which might be more disruptive.

## 2024-05-23 - [Test Construction Pattern]

**Learning:** Controller unit tests instantiate controllers directly via `new Controller(...)` rather than using a test fixture or DI container. This makes constructor injection changes ripple across all test files immediately.
**Action:** When adding dependencies to controllers, expect to update multiple test files manually. Consider introducing a test builder pattern if the pain becomes too high, but respect the current direct instantiation for consistency.

## 2026-02-20 - ErrorViewModel shares namespace with deleted ViewModels

**Learning:** `ErrorViewModel` lives in `Rvnx.CRM.Web.Models` alongside any ViewModels. When deleting a ViewModel from that namespace, the `using Rvnx.CRM.Web.Models` directive must remain if `ErrorViewModel` (or other types) is still referenced in the same file.
**Action:** Before removing a `using` after deleting a class, grep for all types from that namespace in the consuming file.

## 2024-05-24 - [Duplicated Validation Logic]

**Learning:** Business rules for allowed file types were duplicated as a `HashSet` in `AttachmentsController` and a `switch` in `FileValidationService`. This meant adding a new file type required changes in two layers.
**Action:** Centralized the "Allowed Extensions" rule in `FileValidationService` by exposing `IsAllowedExtension`. This makes the service the single source of truth for file validation logic.

## 2024-05-25 - [Test Data Utilities in Core]

**Learning:** `FakeDataGenerator` was located in `Core/Services`, confusing domain logic with development/seeding utilities. Such utilities, even if returning domain entities, belong in `Infrastructure` (for data seeding) or `Tests`.
**Action:** When finding utility classes that generate random data or handle development-only tasks, verify they are not in `Core`. Move them to `Infrastructure` or `Shared` to keep the domain pure.
