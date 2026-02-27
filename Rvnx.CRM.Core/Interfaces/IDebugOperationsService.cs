namespace Rvnx.CRM.Core.Interfaces;

public interface IDebugOperationsService
{
    Task<MergeAccountsResult> MergeAccountsAsync(Guid user1Id, Guid user2Id);
    Task<List<MergeUserDto>> GetAllUsersWithGroupsAsync();
    Task<bool> IsAdministratorAsync(Guid userId);
}

public class MergeAccountsResult
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public string? Error { get; set; }
}

public class MergeUserDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string GroupName { get; set; } = string.Empty;
    public int GroupMemberCount { get; set; }
}
