1. **Optimize GroupBy Allocation Overhead in `ContactExportService.cs`:**
   - In `Rvnx.CRM.Infrastructure/Services/ContactExportService.cs`, the `LoadGroupedAsync<T>` method creates a `Dictionary<Guid, List<T>>` using the LINQ `.GroupBy(keySelector).ToDictionary(g => g.Key, g => g.ToList())` chain.
   - As established in our journal, this introduces unnecessary memory allocation overhead due to intermediate `IGrouping` structures.
   - I will replace this chain with manual pre-allocation of a `Dictionary` followed by a single-pass `foreach` loop that initializes empty lists and `.Add()`s items, bypassing `GroupBy` entirely.
2. **Complete pre-commit steps to ensure proper testing, verification, review, and reflection are done.**
3. **Submit the PR:**
   - Branch: `bolt/optimize-groupby-contactexport`
   - Title: `⚡ Bolt: Optimize GroupBy memory allocations in ContactExportService`
   - Description matching the required format.
