namespace Rvnx.CRM.Core.Interfaces;

public interface IDebugDataService
{
    /// <summary>
    /// Seeds the database with a specified number of random contacts for testing purposes.
    /// </summary>
    /// <param name="count">The number of contacts to generate.</param>
    Task SeedTestDataAsync(int count);

    /// <summary>
    /// Clears all data from the database. Use with caution.
    /// </summary>
    Task ResetDatabaseAsync();

    /// <summary>
    /// Adds random relationships between existing contacts.
    /// </summary>
    /// <param name="maxRelationships">The maximum number of relationships to create.</param>
    /// <returns>The number of relationships actually created.</returns>
    Task<int> AddRandomRelationshipsAsync(int maxRelationships = 50);
}