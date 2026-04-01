1. Modify `Rvnx.CRM.Core/Interfaces/ISelfContactService.cs` using `replace_with_git_merge_diff`
   - Remove `using System.Security.Claims;`
   - Remove `ClaimsPrincipal user` parameters from `GetSelfContactIdAsync`, `GetSelfContactFormAsync`, and `CreateSelfContactAsync`.
2. Modify `Rvnx.CRM.Core/Services/SelfContactService.cs` using `replace_with_git_merge_diff`
   - Remove `using System.Security.Claims;`
   - Remove `ClaimsPrincipal user` parameters.
   - Remove `await _userSynchronizationService.SyncUserAsync(user);` calls.
   - Keep `ICurrentUserService` to get the userId.
   - We can also remove `IUserSynchronizationService` dependency from `SelfContactService` since we no longer sync user here.
3. Modify `Rvnx.CRM.Web/Controllers/ContactsController.cs` using `replace_with_git_merge_diff`
   - Remove passing `HttpContext.User` to `GetSelfContactIdAsync`, `GetSelfContactFormAsync`, and `CreateSelfContactAsync`.
4. Modify `Rvnx.CRM.Tests/Services/SelfContactServiceTests.cs` using `replace_with_git_merge_diff`
   - Update tests to mock the new method signatures (no `ClaimsPrincipal` argument).
   - Remove `_userSynchronizationServiceMock` dependencies from `SelfContactServiceTests`.
5. Modify `Rvnx.CRM.Tests/Services/SelfContactTests.cs` using `replace_with_git_merge_diff`
   - Update tests to not pass `It.IsAny<ClaimsPrincipal>()` to `GetSelfContactIdAsync`.
6. Run `dotnet build` and `dotnet test`.
7. Complete pre-commit steps to ensure proper testing, verification, review, and reflection are done.
