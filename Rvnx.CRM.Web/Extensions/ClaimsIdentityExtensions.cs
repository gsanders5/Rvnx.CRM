using Rvnx.CRM.Core.Constants;
using Rvnx.CRM.Core.Interfaces;
using System.Security.Claims;

namespace Rvnx.CRM.Web.Extensions;

public static class ClaimsIdentityExtensions
{
    public static async Task SyncUserAndEnrichClaimsAsync(this ClaimsIdentity identity, IUserSynchronizationService userSyncService)
    {
        string? subjectId = identity.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? identity.FindFirst("sub")?.Value;
        string? email = identity.FindFirst(ClaimTypes.Email)?.Value ?? identity.FindFirst("email")?.Value;
        string? name = identity.FindFirst(ClaimTypes.Name)?.Value ?? identity.FindFirst("name")?.Value;

        if (!string.IsNullOrEmpty(subjectId))
        {
            UserSyncResult? result = await userSyncService.SyncUserAsync(subjectId, email, name);
            if (result != null)
            {
                Claim? existingUserClaim = identity.FindFirst(ClaimConstants.InternalUserIdClaimType);
                if (existingUserClaim != null)
                {
                    identity.RemoveClaim(existingUserClaim);
                }
                identity.AddClaim(new Claim(ClaimConstants.InternalUserIdClaimType, result.UserId.ToString()));

                Claim? existingGroupClaim = identity.FindFirst(ClaimConstants.InternalGroupIdClaimType);
                if (existingGroupClaim != null)
                {
                    identity.RemoveClaim(existingGroupClaim);
                }
                if (result.GroupId.HasValue)
                {
                    identity.AddClaim(new Claim(ClaimConstants.InternalGroupIdClaimType, result.GroupId.Value.ToString()));
                }

                if (!identity.HasClaim(c => c.Type == ClaimTypes.Name) && !string.IsNullOrEmpty(result.DisplayName))
                {
                    identity.AddClaim(new Claim(ClaimTypes.Name, result.DisplayName));
                }
            }
        }
    }
}
