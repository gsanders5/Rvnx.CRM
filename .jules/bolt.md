# Bolt's Journal

## 2024-05-22 - Polymorphic N+1 Optimization

**Learning:** Polymorphic relationships in EF Core (using `EntityId` and `EntityType`) prevent standard `Include` navigation, leading to manual N+1 loading patterns in controllers.
**Action:** Use a two-step loading process: 1) Fetch the lightweight polymorphic entities (e.g., Relationships), 2) Collect IDs and fetch the related entities in a single batch query using `Contains`, then map them in memory.

## 2024-05-22 - SQL Parameter Limit

**Learning:** Using `Contains` with large lists (>2100 items) in EF Core causes SQL parameter limit exceptions (or performance degradation).
**Action:** When filtering by a large list of IDs, consider fetching a broader set (e.g., all relevant entities) and filtering in memory, especially if the broader set size is comparable to the requested set.

## 2024-05-23 - Memory Leak in Read-Only Lookups

**Learning:** `PopulateRelatedEntityDropdown` was fetching thousands of tracked entities into memory just to filter one out and populate a dropdown.
**Action:** Use `ListAsNoTrackingAsync` with a predicate for read-only lookups to avoid change tracking overhead and filter at the database level.

## 2026-02-22 - Unbounded Collection Fetch in Dashboard

**Learning:** DashboardService was fetching the entire `Reminders` table into memory to filter for active ones.
**Action:** Always apply filters (especially status/completion flags) at the database level using predicates in `ListAsNoTrackingAsync`.

## 2026-02-22 - SQLite Cannot Translate TimeSpan Comparisons

**Learning:** EF Core cannot translate `TimeSpan` comparisons (e.g. `r.EventFrequency > TimeSpan.Zero`) to SQL when targeting SQLite. This causes an `InvalidOperationException` at runtime.
**Action:** Remove `TimeSpan` comparisons from DB-level predicates. Apply them in memory after fetching, or rely on existing in-memory loop logic to skip unwanted records.

## 2026-02-22 - Stale SQLite DB from Relative Connection String

**Learning:** Using a relative path in the SQLite connection string (e.g. `Data Source=rvnx-crm.db`) resolves differently depending on the working directory (`dotnet run` vs `dotnet ef`), leading to multiple stale `.db` files and schema-out-of-sync errors (e.g. `no such column`).
**Action:** Use an absolute path in `appsettings.Local.json`. When a model change adds columns, delete stale `.db` files (check both the project root and `bin/`) and run `dotnet ef database update --project Rvnx.CRM.Infrastructure --startup-project Rvnx.CRM.Web`.

## 2026-06-25 - Over-fetching in Relationships Display

**Learning:** `GetContactDetailsAsync` was fetching full `Contact` entities (including large text fields like Notes/Bio if present on base class, though `ListAsNoTrackingAsync` fetches all columns) just to display names and links in the relationships list.
**Action:** Use `ListProjectedByChunkedContainsAsync` to fetch only the necessary columns (`Id`, `FirstName`, `LastName`, `Gender`, `IsPartial`) into a lightweight object, significantly reducing data transfer. Note that unit tests mocking this must setup `ListProjectedAsync`, not `ListAsNoTrackingAsync`.

## 2026-06-26 - Helper Method Over-fetching

**Learning:** Common helper methods like `GetEntityName` and `IsPartialContactAsync` were fetching entire entities (via `GetByIdAsync`) just to retrieve a single string or boolean property. This adds unnecessary I/O overhead for every controller action that uses them.
**Action:** Replace `GetByIdAsync` with `ListProjectedAsync` in read-only helper methods to fetch only the specific columns needed (e.g., `FirstName`, `LastName`, `IsPartial`). Ensure unit tests mocking these helpers are updated to mock the projection method.
