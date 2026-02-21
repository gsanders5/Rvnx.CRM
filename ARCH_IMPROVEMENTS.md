# Cartographer Architecture Recommendations

This document outlines prioritized architectural improvements based on mapping the actual `Rvnx.CRM` codebase against its stated design.

## 🔴 High Priority

### 1. Move domain orchestration services to Core

- **Finding:** Services like `ContactManagementService`, `ContactReadService`, `SelfContactService`, and `DashboardService` are located in `Rvnx.CRM.Infrastructure`.
- **Impact:** This violates Clean Architecture. These services orchestrate domain logic using `IRepository` (which is in Core) and do not directly depend on infrastructure concerns. Treating them as infrastructure makes the domain layer anemic and confuses the dependency graph.
- **Recommendation:** Move these services to `Rvnx.CRM.Core/Services`. Only services with genuine external dependencies (e.g., `VCardService`, `UserSynchronizationService`) or direct DB access logic (`Repository`) should remain in Infrastructure.
- **Effort:** Medium

### 2. Extract business logic from thick controllers

- **Finding:** `RelationshipsController` (and other controllers) manually handle polymorphic mapping logic, string splitting (e.g., parsing `SelectedRelationshipType`), and reversing directionality for relationships.
- **Impact:** Controllers have more than one reason to change, are difficult to unit-test comprehensively, and tie business rules to HTTP concerns.
- **Recommendation:** Extract this manual mapping and relationship management logic into a dedicated domain service (e.g., `IRelationshipService`). Controllers should only handle HTTP context, model binding, and delegating to services.
- **Effort:** Large

## 🟡 Medium Priority

### 3. Stop controllers from bypassing the service layer

- **Finding:** Controllers frequently inherit from `RepositoryController` and inject `IRepository` directly to perform CRUD operations (e.g., `_repository.AddAsync(relationship)`).
- **Impact:** Bypassing application services means any cross-cutting business rules, validation, or side-effects cannot be easily enforced across the board, leading to duplication or missed rules when entities are modified. The stated design says controllers delegate to specialized services, but reality diverges.
- **Recommendation:** Introduce specific application services for CRUD operations on entities rather than allowing controllers open access to `IRepository`. Controllers should inject these UI-agnostic services instead.
- **Effort:** Large

### 4. Reorganize the Tests project structure

- **Finding:** `Rvnx.CRM.Tests` contains all unit and integration tests flatly in the root folder, mixed together.
- **Impact:** It is difficult to navigate the test suite, ascertain coverage for specific layers, or distinguish between unit and integration boundaries.
- **Recommendation:** Restructure the test project folders to mirror the production code structure (e.g., `/Core`, `/Infrastructure`, `/Web`) and clearly isolate integration tests.
- **Effort:** Small

## 🟢 Low Priority

### 5. Standardize cross-cutting concerns (Logging)

- **Finding:** Logging is applied consistently in some controllers (e.g., `ContactsController` takes `ILogger<ContactsController>`) but completely omitted in others (e.g., `RelationshipsController`).
- **Impact:** Troubleshooting and observability will be erratic depending on which feature is throwing an exception.
- **Recommendation:** Implement a consistent error handling and logging strategy across all controllers, potentially utilizing a global exception filter or standardizing base controller logging.
- **Effort:** Small
