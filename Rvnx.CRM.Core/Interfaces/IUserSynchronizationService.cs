namespace Rvnx.CRM.Core.Interfaces;

public class UserSyncResult
{
    public Guid UserId { get; set; }
    public Guid? GroupId { get; set; }
    public string? DisplayName { get; set; }
}

public interface IUserSynchronizationService
{
    /// <summary>
    /// Synchronizes the authenticated user's details (ID, Name, Email) with the local User database table.
    /// Creates the user record if it doesn't exist, or updates it if changed.
    /// </summary>
    /// <param name="subjectId">The subject identifier from the authentication provider.</param>
    /// <param name="email">The user's email address.</param>
    /// <param name="name">The user's display name.</param>
    /// <returns>A result containing the user's internal IDs and display name.</returns>
    Task<UserSyncResult?> SyncUserAsync(string subjectId, string? email, string? name);
}
