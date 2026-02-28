namespace Rvnx.CRM.Core.Interfaces;

public interface ICurrentUserService
{
    /// <summary>
    /// Gets the unique identifier of the currently authenticated user.
    /// Returns null if the user is not authenticated.
    /// </summary>
    Guid? UserId { get; }

    /// <summary>
    /// Gets the Group ID associated with the current user.
    /// Used for data isolation (multi-tenancy).
    /// </summary>
    Guid? GroupId { get; }

    /// <summary>
    /// Gets the username or email of the current user.
    /// Returns "System" if not authenticated.
    /// </summary>
    string? UserName { get; }

    /// <summary>
    /// Indicates whether the current request is authenticated.
    /// </summary>
    bool IsAuthenticated { get; }

    /// <summary>
    /// Checks if a user has administrator privileges.
    /// </summary>
    /// <param name="userId">The ID of the user to check.</param>
    /// <returns>True if the user is an administrator; otherwise, false.</returns>
    Task<bool> IsAdministratorAsync(Guid userId);
}
