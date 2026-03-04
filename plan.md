1. **Goal:** Improve test coverage by adding missing tests. Based on the philosophy of Vigil, adding tests for missing edge cases or domain rules is highly valuable.
2. **Current state:** `Rvnx.CRM.Tests.Repositories.RepositoryTests` only tests `ListAsync`, `ListAsNoTrackingAsync` (predicate+includes), and `CountAsync`. It lacks tests for `ListProjectedAsync`, which is a key performance optimization method heavily used across the application.
3. **Plan:**
   - Add tests to `Rvnx.CRM.Tests/Repositories/RepositoryTests.cs` for `ListProjectedAsync`.
   - Add `ListProjectedAsync_ReturnsProjectedData` to test the basic projection logic.
   - Add `ListProjectedAsync_WithOrderBy_OrdersDataCorrectly` to test the overload with sorting.
4. **Pre-commit:** Run the `pre_commit_instructions` tool to make sure all verification, testing, and reflection steps are executed.
