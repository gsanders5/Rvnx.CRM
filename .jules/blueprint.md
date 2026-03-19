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

## 2026-05-26 - [Controller Orchestrating Data Operations]

**Learning:** `DebugOperationsController` contained significant data manipulation logic (seeding, reset) directly in action methods, coupling the web layer to concrete infrastructure utilities (`FakeDataGenerator`) and orchestrating complex repository operations.
**Action:** Extract such logic into a dedicated service (e.g., `IDebugDataService` in Core, implementation in Infrastructure) so the controller only delegates commands. This decouples the web layer from implementation details of data management.
## 2023-10-27 - [Infrastructure Leak in Web Layer]
**Learning:** `DbUpdateConcurrencyException` (and `Microsoft.EntityFrameworkCore`) was leaked into the Web layer (`SignificantDatesController`). The controller was catching it just to rethrow (`throw;`), which is both redundant and violates clean architecture by making the presentation layer depend directly on the ORM.
**Action:** When working on controllers or core domain services, explicitly check for `using Microsoft.EntityFrameworkCore;` or explicit usage of `DbUpdateConcurrencyException`. Remove these where possible, and if handling is needed, ensure the infrastructure layer (or service layer) wraps it in a domain-friendly `OperationResult` or custom exception.
## 2024-05-27 - [Infrastructure Dependency in Web Services]
**Learning:** Services located in the `Web` layer (like `UserClaimsTransformation` and `CurrentUserService`) were directly referencing `Microsoft.EntityFrameworkCore` to call `.FirstOrDefaultAsync()` on `IQueryable` results from the repository. This coupled the Web layer directly to the ORM.
**Action:** Replaced direct EF Core async extensions with appropriate `IRepository` abstraction methods (e.g., `GetByIdAsync`, `ListAsNoTrackingAsync().FirstOrDefault()`) to remove the EF Core using directives and strict dependencies from the Web layer, restoring the clean boundary.
## 2024-05-28 - [Extract Business Logic from View Helper to Core Service]

**Learning:** String-manipulation logic (`ExtractUsername` for social media URIs) incorrectly resided in the `Web` layer (`SocialMediaEmbedHelper`) despite being a domain-level data normalization rule.
**Action:** When finding logic that normalizes or validates strings representing domain concepts, verify it lives in `Core` (like `SocialMediaUrlNormalizer`). Move it if it's currently in a Web layer helper to centralize the rules.
## 2025-03-20 - Removed EF Core attributes from `Fact` domain model in `Core`
**Learning:** Found an instance where an EF Core model (`Fact`) in the `Core` layer was polluted with presentation (`[Display]`) and persistence (`[Table]`, `[ForeignKey]`, `[Required]`, `[MaxLength]`) attributes, which violated the clean separation of concerns and layered architecture dependency rules.
**Action:** When a domain entity in `Core` is decorated with infrastructure or presentation attributes, remove them from the entity and configure them using the Fluent API in the corresponding DbContext inside the `Infrastructure` layer to keep `Core` decoupled and pure.
