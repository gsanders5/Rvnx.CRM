1. **Add Tests for `GetFavoriteSidebarItemsAsync` in `FavoriteServiceTests.cs`:**
   - Use `replace_with_git_merge_diff` to add a nested class `GetFavoriteSidebarItemsAsyncTests` into `FavoriteServiceTests` (since the current class structure doesn't use nested classes, we will refactor it to include `ToggleFavoriteAsyncTests` and `GetFavoriteContactIdsAsyncTests` alongside `GetFavoriteSidebarItemsAsyncTests` to adhere to the nested class convention for new tests, or just add the new tests without nested classes if the file doesn't currently use them and the memory says "Service unit tests in `Rvnx.CRM.Tests` use nested classes to group tests by method name. When adding new tests, follow this convention (e.g., `public class [ServiceName][MethodName]Tests`)"). Let's add the nested class `FavoriteServiceGetFavoriteSidebarItemsAsyncTests` following the memory instruction explicitly. Wait, the memory says `public class [ServiceName][MethodName]Tests`.
   - The new class `FavoriteServiceGetFavoriteSidebarItemsAsyncTests` will contain the following test methods:
     - `WhenUserIdNull_ReturnsEmptyList()`: Verifies that if `_currentUserService.UserId` is null, it immediately returns an empty list.
     - `WhenNoFavorites_ReturnsEmptyList()`: Verifies that if the first query `ListProjectedAsync<ContactFavorite, Guid>` returns an empty list, it returns an empty list without making further queries.
     - `WhenContactsFilteredOut_ReturnsEmptyList()`: Verifies that if the second query (`ListProjectedByChunkedContainsAsync` for contacts) returns an empty list, it returns an empty list.
     - `WithValidFavorites_ReturnsSortedItemsWithProfileImages()`: Verifies the happy path. It mocks the contact favorite query to return IDs, the contact query to return unsorted `FavoriteSidebarItemDto`s, and the attachment query to return a mapped tuple. Finally, asserts the items are sorted correctly (by FirstName, then LastName) and that `ProfileImageId` is populated correctly.
2. **Journal update:**
   - Use `run_in_bash_session` to append an entry to `.jules/vigil.md` explaining the discovery of untested complex data aggregation and projection methods (like `GetFavoriteSidebarItemsAsync`) that risk breaking the UI if modified without tests.
3. **Verification:**
   - Use `run_in_bash_session` to run `dotnet build Rvnx.CRM.slnx` and `dotnet test Rvnx.CRM.slnx` to confirm the new tests pass and do not break the build.
4. **Pre-commit:**
   - Complete pre-commit steps to ensure proper testing, verification, review, and reflection are done.
5. **Submission:**
   - Submit the PR as `🔍 Vigil: Added tests for GetFavoriteSidebarItemsAsync`.
