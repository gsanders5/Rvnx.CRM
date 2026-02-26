using Rvnx.CRM.Core.DTOs.Base;
using Rvnx.CRM.Core.Models;

namespace Rvnx.CRM.Core.Interfaces;

public interface IDebugDataService
{
    Task SeedTestDataAsync(int count);
    Task ResetDatabaseAsync();
    Task<int> AddRandomRelationshipsAsync(int maxRelationships = 50);

    Task<List<User>> GetMergeCandidatesAsync();
    Task<MergeOperationResult> MergeAccountsAsync(Guid user1Id, Guid user2Id, Guid adminId);
    Task<bool> IsAdministratorAsync(Guid userId);
}
