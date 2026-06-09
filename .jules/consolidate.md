## 2026-05-30 - Fix CI Format Error After Test Consolidation
**Learning:** Sometimes files outside of the target test folder might fail CI formatting checks due to pre-existing import issues or global format updates from `dotnet format`.
**Action:** Always run `dotnet format` to catch unrelated formatting errors that might block CI builds.
