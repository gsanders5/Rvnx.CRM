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

## 2026-04-16 - [Controller Orchestrating Data Operations]

**Learning:** `DebugOperationsController` contained significant data manipulation logic (seeding, reset) directly in action methods, coupling the web layer to concrete infrastructure utilities (`FakeDataGenerator`) and orchestrating complex repository operations.
**Action:** Extract such logic into a dedicated service (e.g., `IDebugDataService` in Core, implementation in Infrastructure) so the controller only delegates commands. This decouples the web layer from implementation details of data management.
## 2023-10-27 - [Infrastructure Leak in Web Layer]
**Learning:** `DbUpdateConcurrencyException` (and `Microsoft.EntityFrameworkCore`) was leaked into the Web layer (`SignificantDatesController`). The controller was catching it just to rethrow (`throw;`), which is both redundant and violates clean architecture by making the presentation layer depend directly on the ORM.
**Action:** When working on controllers or core domain services, explicitly check for `using Microsoft.EntityFrameworkCore;` or explicit usage of `DbUpdateConcurrencyException`. Remove these where possible, and if handling is needed, ensure the infrastructure layer (or service layer) wraps it in a domain-friendly `OperationResult` or custom exception.
## 2024-05-27 - [Infrastructure Dependency in Web Services]
**Learning:** Services located in the `Web` layer (like `UserClaimsTransformation` and `CurrentUserService`) were directly referencing `Microsoft.EntityFrameworkCore` to call `.FirstOrDefaultAsync()` on `IQueryable` results from the repository. This coupled the Web layer directly to the ORM.
**Action:** Replaced direct EF Core async extensions with appropriate `IRepository` abstraction methods (e.g., `GetByIdAsync`, `ListAsNoTrackingAsync().FirstOrDefault()`) to remove the EF Core using directives and strict dependencies from the Web layer, restoring the clean boundary.
## 2024-05-28 - [Extract Business Logic from View Helper to Core Service]

**Learning:** String-manipulation logic (`ExtractUsername` for social media URIs) was moved from the `Web` layer to `SocialMediaUrlNormalizer` in `Core`, where it belongs as a domain-level data normalization rule. The old `SocialMediaEmbedHelper` no longer exists.
**Action:** When finding logic that normalizes or validates strings representing domain concepts, verify it lives in `Core` (like `SocialMediaUrlNormalizer`). Move it if it's currently in a Web layer helper to centralize the rules.
## 2026-03-29 - [Extract Infrastructure Claims from Core Interface]
**Learning:** The `System.Security.Claims.ClaimsPrincipal` is an ASP.NET Core infrastructure/web type that was incorrectly leaked into the `ISelfContactService` domain interface, coupling the Core layer to identity abstractions.
**Action:** When a domain service requires user information, do not pass `ClaimsPrincipal`. Instead, orchestrate claims synchronization in the Web layer (Controllers) and pass primitive identifiers, or rely entirely on a domain abstraction like `ICurrentUserService` to retrieve the authenticated user's state.
## 2025-03-03 - Core Domain Logic Independence
**Learning:** Sometimes `ClaimsPrincipal` leaks into the `Core` logic. Domain logic shouldn't care about HttpContext or web identities. Keep domain interfaces clean and rely on mapping to primitives or ICurrentUserService in the Web/Controllers layer instead.
**Action:** When working on services in `Rvnx.CRM.Core/Interfaces`, never introduce properties/parameters of type `ClaimsPrincipal`. Extract necessary IDs/Strings in the controller layer or use `ICurrentUserService`.
## 2024-06-05 - [Duplicate User Synchronization Call]
**Learning:** `IUserSynchronizationService.SyncUserAsync` is correctly invoked globally via authentication middleware in `Program.cs` for every authenticated request. Redundant calls to this service from within individual controller actions (e.g., `ContactsController.CreateSelf`) are unnecessary and unnecessarily couple the controller to the synchronization service.
**Action:** When auditing controllers for coupling, identify and remove explicit invocations of `IUserSynchronizationService` to rely entirely on the global middleware.
## 2026-04-12 - [Feature Specific View Models]
**Learning:** In Rvnx.CRM.Web, ViewModels shouldn't be placed directly in the generic `Models/` folder if they are feature-specific. Instead, they should be grouped into feature-specific subdirectories under `ViewModels/` (e.g., `ViewModels/DebugOperations`, `ViewModels/Merge`) to improve code modularity and separation of concerns.
**Action:** Always place newly created ViewModels in a feature-specific subdirectory within `ViewModels/` rather than leaving them at the root or within a generic models folder.
## 2025-04-15 - [Decouple Web Identity from Core Interfaces]
**Learning:** `ClaimsPrincipal` was previously being passed directly to `IUserSynchronizationService` in the `Core` layer, and the `Infrastructure` implementation was reading and mutating claims directly. This leaked `System.Security.Claims` and Web-specific authentication concerns into the inner layers, violating the dependency rule.
**Action:** Always extract primitive identifiers (`subjectId`, `email`, `name`) in the `Web` layer (like in `Program.cs` or controllers) and pass those primitives to `Core` interfaces. Return DTOs (`UserSyncResult`) to the `Web` layer so it can handle its own identity mutations (adding internal ID claims).
## 2024-06-08 - [Extract Repeated Claims Enrichment Logic]
**Learning:** Claims enrichment logic (fetching user details, adding internal app claims) was duplicated exactly across two locations in `Program.cs` (`OnTokenValidated` and the custom auth middleware). This violates DRY and litters the app bootstrapping file with verbose business logic.
**Action:** When finding repeated logic acting on ASP.NET Core primitives (like `ClaimsIdentity` or `ClaimsPrincipal`) inside `Program.cs` or controllers, extract it to a cleanly separated Extension Method within the `Web` project (e.g., `Rvnx.CRM.Web/Extensions`). This keeps the host configuration clean and focuses the identity logic in one reusable location.
