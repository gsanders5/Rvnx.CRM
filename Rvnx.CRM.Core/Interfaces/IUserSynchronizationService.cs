using System.Security.Claims;

namespace Rvnx.CRM.Core.Interfaces;

public interface IUserSynchronizationService
{
    /// <summary>
    /// Synchronizes the authenticated user's claims (ID, Name, Email) with the local User database table.
    /// Creates the user record if it doesn't exist, or updates it if changed.
    /// </summary>
    /// <param name="principal">The claims principal from the authentication provider.</param>
    Task SyncUserAsync(ClaimsPrincipal principal);
}