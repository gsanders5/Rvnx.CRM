using Microsoft.EntityFrameworkCore;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models;
using Rvnx.CRM.Infrastructure.Data;
using System.Security.Claims;

namespace Rvnx.CRM.Web.Services;

public class UserSynchronizationService : IUserSynchronizationService
{
    private readonly CRMDbContext _dbContext;

    /// <summary>
    /// Custom claim type for the internal CRM user ID.
    /// Using a separate claim type avoids side effects of modifying NameIdentifier,
    /// which other middleware or logging systems may have already read.
    /// </summary>
    public const string InternalUserIdClaimType = "urn:crm:internal-user-id";

    public UserSynchronizationService(CRMDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task SyncUserAsync(ClaimsPrincipal principal)
    {
        string? subject = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
                   ?? principal.FindFirst("sub")?.Value;

        string? email = principal.FindFirst(ClaimTypes.Email)?.Value
                 ?? principal.FindFirst("email")?.Value;

        string? name = principal.FindFirst(ClaimTypes.Name)?.Value
                ?? principal.FindFirst("name")?.Value;

        if (string.IsNullOrEmpty(subject))
        {
            return; // Cannot sync without a subject identifier
        }

        User? user = await _dbContext.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.SubjectId == subject);

        if (user == null)
        {
            user = new User
            {
                SubjectId = subject,
                Email = email ?? "unknown@example.com",
                DisplayName = name ?? email,
                CreatedBy = "System",
                LastChangedBy = "System",
            };
            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();
            user.UserId = user.Id;
            await _dbContext.SaveChangesAsync();
        }
        else
        {
            bool changed = false;
            if (email != null && user.Email != email)
            {
                user.Email = email;
                changed = true;
            }
            if (name != null && user.DisplayName != name)
            {
                user.DisplayName = name;
                changed = true;
            }

            if (changed) await _dbContext.SaveChangesAsync();
        }

        // Add internal user ID as a separate claim instead of replacing NameIdentifier
        // This preserves the original external identity for logging/auditing while
        // providing the internal ID for CRM operations
        if (principal.Identity is ClaimsIdentity identity)
        {
            // Remove any existing internal user ID claim (in case of re-sync)
            Claim? existingInternalClaim = identity.FindFirst(InternalUserIdClaimType);
            if (existingInternalClaim != null)
            {
                identity.RemoveClaim(existingInternalClaim);
            }

            // Add the internal user ID claim
            identity.AddClaim(new Claim(InternalUserIdClaimType, user.Id.ToString()));

            // Add display name if not already present
            if (!identity.HasClaim(c => c.Type == ClaimTypes.Name) && !string.IsNullOrEmpty(user.DisplayName))
            {
                identity.AddClaim(new Claim(ClaimTypes.Name, user.DisplayName));
            }
        }
    }
}