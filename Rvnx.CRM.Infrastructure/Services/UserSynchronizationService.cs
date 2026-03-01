using Microsoft.EntityFrameworkCore;
using Rvnx.CRM.Core.Constants;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models;
using Rvnx.CRM.Infrastructure.Data;
using System.Security.Claims;

namespace Rvnx.CRM.Infrastructure.Services;

public class UserSynchronizationService(CRMDbContext dbContext, IRepository repository) : IUserSynchronizationService
{
    private readonly CRMDbContext _dbContext = dbContext;
    private readonly IRepository _repository = repository;

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

        User? user = await _repository.QueryUnfiltered<User>().FirstOrDefaultAsync(u => u.SubjectId == subject);

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

            UserGroup group = new()
            {
                Name = user.DisplayName ?? "My Group",
                CreatedBy = "System",
                LastChangedBy = "System"
            };
            user.Group = group;

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

            if (user.GroupId == null)
            {
                UserGroup group = new()
                {
                    Name = user.DisplayName ?? "My Group",
                    CreatedBy = "System",
                    LastChangedBy = "System"
                };
                user.Group = group;
                changed = true;
            }

            if (changed)
            {
                await _dbContext.SaveChangesAsync();
            }
        }

        // Add internal user ID as a separate claim instead of replacing NameIdentifier
        // This preserves the original external identity for logging/auditing while
        // providing the internal ID for CRM operations
        if (principal.Identity is ClaimsIdentity identity)
        {
            Claim? existingUserClaim = identity.FindFirst(ClaimConstants.InternalUserIdClaimType);
            if (existingUserClaim != null)
            {
                identity.RemoveClaim(existingUserClaim);
            }
            identity.AddClaim(new Claim(ClaimConstants.InternalUserIdClaimType, user.Id.ToString()));

            Claim? existingGroupClaim = identity.FindFirst(ClaimConstants.InternalGroupIdClaimType);
            if (existingGroupClaim != null)
            {
                identity.RemoveClaim(existingGroupClaim);
            }

            if (user.GroupId.HasValue)
            {
                identity.AddClaim(new Claim(ClaimConstants.InternalGroupIdClaimType, user.GroupId.Value.ToString()));
            }

            if (!identity.HasClaim(c => c.Type == ClaimTypes.Name) && !string.IsNullOrEmpty(user.DisplayName))
            {
                identity.AddClaim(new Claim(ClaimTypes.Name, user.DisplayName));
            }
        }
    }
}