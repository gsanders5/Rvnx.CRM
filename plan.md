1. Use `replace_with_git_merge_diff` to restructure `Rvnx.CRM.Tests/Services/DateCalculationServiceTests.cs` into nested classes (`GetNextOccurrence`, `GetCurrentYearOccurrence`, `GetScheduledForDate`) and add new tests `GetNextOccurrenceCustomWithNegativeIntervalReturnsEventDate` and `GetCurrentYearOccurrenceFeb29LeapYearReturnsFeb29` simultaneously.
2. Run `dotnet build` and `dotnet test Rvnx.CRM.Tests/Rvnx.CRM.Tests.csproj` using `run_in_bash_session` to verify the build succeeds and all tests pass.
3. Complete pre-commit steps to ensure proper testing, verification, review, and reflection are done.
4. Submit the PR with title "🔍 Vigil: Added explicit coverage for negative recurrences and leap year clamping in DateCalculationService" using the `submit` tool.
