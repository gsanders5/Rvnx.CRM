using Microsoft.EntityFrameworkCore;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models;
using Rvnx.CRM.Infrastructure.Data;
using System.Security.Claims;

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
        string? subject = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
                   ?? principal.FindFirst("sub")?.Value;

        string? email = principal.FindFirst(ClaimTypes.Email)?.Value
                 ?? principal.FindFirst("email")?.Value;

        string? name = principal.FindFirst(ClaimTypes.Name)?.Value
                ?? principal.FindFirst("name")?.Value;

        if (!string.IsNullOrEmpty(subject))
        {
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

            ClaimsIdentity identity = (ClaimsIdentity) principal.Identity!;
            Claim? nameIdClaim = identity.FindFirst(ClaimTypes.NameIdentifier);
            if (nameIdClaim != null)
            {
                identity.RemoveClaim(nameIdClaim);
            }
            identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()));

            if (!identity.HasClaim(c => c.Type == ClaimTypes.Name) && !string.IsNullOrEmpty(user.DisplayName))
            {
                identity.AddClaim(new Claim(ClaimTypes.Name, user.DisplayName));
            }
        }
    }
}
