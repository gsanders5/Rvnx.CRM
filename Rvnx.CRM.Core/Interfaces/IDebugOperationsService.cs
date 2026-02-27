namespace Rvnx.CRM.Core.Interfaces;

public interface IDebugOperationsService
{
    /// <summary>
    /// Merges two user accounts, transferring all data/group ownership from the second user to the first.
    /// </summary>
    /// <param name="user1Id">The ID of the target user (who will keep the data).</param>
    /// <param name="user2Id">The ID of the source user (who will be deleted).</param>
    /// <returns>A <see cref="MergeAccountsResult"/> indicating success or failure.</returns>
    Task<MergeAccountsResult> MergeAccountsAsync(Guid user1Id, Guid user2Id);

    /// <summary>
    /// Retrieves a list of all users and their group membership statistics.
    /// </summary>
    /// <returns>A list of <see cref="MergeUserDto"/>.</returns>
    Task<List<MergeUserDto>> GetAllUsersWithGroupsAsync();

    /// <summary>
    /// Checks if a user has administrator privileges.
    /// </summary>
    /// <param name="userId">The ID of the user to check.</param>
    /// <returns>True if the user is an administrator; otherwise, false.</returns>
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
