using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models;
using Rvnx.CRM.Infrastructure.Data;

namespace Rvnx.CRM.Web.Services;

public class UserSynchronizationService : IUserSynchronizationService
{
    private readonly CRMDbContext _dbContext;

    public UserSynchronizationService(CRMDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task SyncUserAsync(ClaimsPrincipal principal)
    {
        var subject = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
                   ?? principal.FindFirst("sub")?.Value;

        var email = principal.FindFirst(ClaimTypes.Email)?.Value
                 ?? principal.FindFirst("email")?.Value;

        var name = principal.FindFirst(ClaimTypes.Name)?.Value
                ?? principal.FindFirst("name")?.Value;

        if (!string.IsNullOrEmpty(subject))
        {
            // Lookup user using IgnoreQueryFilters to ensure we can see all users (if User table was filtered, but it's not)
            // Still good practice in case we add filter later.
            var user = await _dbContext.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.SubjectId == subject);

            if (user == null)
            {
                user = new User
                {
                    SubjectId = subject,
                    Email = email ?? "unknown@example.com",
                    DisplayName = name ?? email,
                    CreatedBy = "System",
                    LastChangedBy = "System",
                    UserId = "System" // System owns the user record mapping
                };
                _dbContext.Users.Add(user);
                await _dbContext.SaveChangesAsync();
            }
            else
            {
                // Optional: Update user details if changed
                if (user.Email != email || user.DisplayName != name)
                {
                    if (email != null) user.Email = email;
                    if (name != null) user.DisplayName = name;
                    await _dbContext.SaveChangesAsync();
                }
            }

            // Map external subject to internal User Id (Guid)
            var identity = (ClaimsIdentity)principal.Identity!;
            var nameIdClaim = identity.FindFirst(ClaimTypes.NameIdentifier);
            if (nameIdClaim != null)
            {
                identity.RemoveClaim(nameIdClaim);
            }
            identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()));

            // Ensure Name claim is set for display
            if (!identity.HasClaim(c => c.Type == ClaimTypes.Name) && !string.IsNullOrEmpty(user.DisplayName))
            {
                 identity.AddClaim(new Claim(ClaimTypes.Name, user.DisplayName));
            }
        }
    }
}
