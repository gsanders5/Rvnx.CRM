## 2026-05-30 - Fix CI Format Error After Test Consolidation
**Learning:** Sometimes files outside of the target test folder might fail CI formatting checks due to pre-existing import issues or global format updates from `dotnet format`.
**Action:** Always run `dotnet format` to catch unrelated formatting errors that might block CI builds.
## 2026-05-30 - Test entries log pollution
**Learning:** `dotnet test --list-tests` includes standard output logs (such as project restore times) which can cause line count (`wc -l`) mismatches.
**Action:** Use `diff` on the command outputs or grep out non-test entries to accurately verify that the actual test entries remain identical.
