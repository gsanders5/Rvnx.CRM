1. **Understand the problem**: `ContactReadService.GetIntroducerCandidatesAsync` is completely uncovered by unit tests. It's a method that gets a list of full contact names, filters out partial contacts and optionally the provided `excludeContactId`, and sorts them alphabetically by name.
2. **Review implementation details**:
```csharp
    public async Task<List<ContactSelectItemDto>> GetIntroducerCandidatesAsync(Guid? excludeContactId)
    {
        // Tenancy is enforced by the global query filter; only exclude self (when known) and partial contacts.
        List<ContactSelectItemDto> candidates = await _repository.ListProjectedAsync<Contact, ContactSelectItemDto>(
            c => !c.IsPartial && (excludeContactId == null || c.Id != excludeContactId),
            c => new ContactSelectItemDto
            {
                Id = c.Id,
                FullName = (c.FirstName + " " + (c.LastName ?? "")).Trim()
            }) ?? [];
        return [.. candidates.OrderBy(x => x.FullName)];
    }
```
3. **Plan tests**:
Create a nested test class `GetIntroducerCandidatesAsyncTests` in `Rvnx.CRM.Tests/Services/ContactReadServiceTests.cs`.
- Happy Path: Returns full contacts and maps FullName correctly, then orders by name.
- Edge Case: Filters out partial contacts.
- Edge Case: Filters out the excluded contact ID (when provided).
- Edge Case: Doesn't filter out any ID when `excludeContactId` is null.

4. **Verify implementation**: Run `dotnet test Rvnx.CRM.Tests/Rvnx.CRM.Tests.csproj`.
