namespace Rvnx.CRM.Core.Interfaces;

public interface IDebugDataService
{
    Task SeedTestDataAsync(int count);
    Task ResetDatabaseAsync();
    Task<int> AddRandomRelationshipsAsync(int maxRelationships = 50);
}
