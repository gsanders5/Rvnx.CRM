# Vigil's Journal

## 2026-02-19 - Initial Setup

**Learning:** Foundational journal for tracking critical testing insights in the Rvnx.CRM application.
**Action:** Let's focus on meaningful tests according to Vigil's philosophy.

## 2026-02-19 - Missing Failure Path Context Preservation Tests

**Learning:** Many POST actions in controllers (like `RelationshipsController.Create`) have logic that gracefully returns `View(dto)` when validation fails. However, if tests do not verify that reference data (`SelectLists`, `ViewData` identifiers) are re-populated on this failure path, future refactoring could easily drop the re-population code. This would cause the rendered View to silently crash in production with a `NullReferenceException` when it attempts to rebuild its dropdowns.
**Action:** Always add a failure-path validation test for any POST endpoint that returns a View populated with drop-downs. Assert that `ModelState.IsValid` is false AND importantly, that `ViewData["<YourSelectList>"]` is `NotNull` when the `ViewResult` is returned.
