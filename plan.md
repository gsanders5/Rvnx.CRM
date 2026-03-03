1. **Create shared helper class**
   - Create `Rvnx.CRM.Core/Services/ContactHelper.cs` (or potentially an extension method, or a static helper). Since `UpdateOrAddContactMethod` and `UpdateOrAddBirthday` heavily use `IRepository`, it would be best to make it a static helper method taking `IRepository` as a parameter.
   - Wait, `IRepository` is already injected in `ContactManagementService` and `SelfContactService`.
   - I can create an internal static class `ContactUpdateHelper` in `Rvnx.CRM.Core/Services/` that provides `UpdateOrAddContactMethodAsync(IRepository repository, Guid contactId, ContactMethodType type, string? newValue, ContactMethod? existingMethod)` and `UpdateOrAddBirthdayAsync(IRepository repository, Guid contactId, DateTime? newDate, SignificantDate? existingDate, bool remindOnBirthday)`.
2. **Move duplicated logic**
   - Copy the logic of `UpdateOrAddContactMethod` and `UpdateOrAddBirthday` to the new `ContactUpdateHelper` methods.
3. **Refactor `ContactManagementService.cs`**
   - Remove the `UpdateOrAddContactMethod` and `UpdateOrAddBirthday` methods from `ContactManagementService.cs`.
   - Update usages to call `ContactUpdateHelper.UpdateOrAddContactMethodAsync` and `ContactUpdateHelper.UpdateOrAddBirthdayAsync`.
4. **Refactor `SelfContactService.cs`**
   - Remove the `UpdateOrAddContactMethod` and `UpdateOrAddBirthday` methods from `SelfContactService.cs`.
   - Update usages to call `ContactUpdateHelper.UpdateOrAddContactMethodAsync` and `ContactUpdateHelper.UpdateOrAddBirthdayAsync`.
5. **Run tests**
   - Run `dotnet build` and `dotnet test`.
6. **Pre-commit and submit**
   - Run pre-commit instructions, submit PR.
