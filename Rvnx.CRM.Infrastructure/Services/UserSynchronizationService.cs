using Microsoft.EntityFrameworkCore;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models;
using Rvnx.CRM.Infrastructure.Data;

namespace Rvnx.CRM.Infrastructure.Services;

public class UserSynchronizationService(CRMDbContext dbContext, IRepository repository) : IUserSynchronizationService
{
    private readonly CRMDbContext _dbContext = dbContext;
    private readonly IRepository _repository = repository;

    public async Task<UserSyncResult?> SyncUserAsync(string subjectId, string? email, string? name)
    {
        if (string.IsNullOrEmpty(subjectId))
        {
            return null; // Cannot sync without a subject identifier
        }

        User? user = await _repository.QueryUnfiltered<User>().FirstOrDefaultAsync(u => u.SubjectId == subjectId);

        if (user == null)
        {
            user = new User
            {
                SubjectId = subjectId,
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

            _dbContext.Users!.Add(user);
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
                _dbContext.UserGroups!.Add(group);
                user.GroupId = group.Id;
                // Explicit Update required: setting only a FK scalar on a tracked entity
                // does not reliably mark it Modified in EF InMemory without this call.
                _dbContext.Users!.Update(user);
                changed = true;
            }

            if (changed)
            {
                await _dbContext.SaveChangesAsync();
            }
        }

        return new UserSyncResult
        {
            UserId = user.Id,
            GroupId = user.GroupId,
            DisplayName = user.DisplayName
        };
    }
}
