using System.Security.Claims;

namespace Rvnx.CRM.Core.Interfaces;

public interface IUserSynchronizationService
{
    Task SyncUserAsync(ClaimsPrincipal principal);
}
