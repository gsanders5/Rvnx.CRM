using Rvnx.CRM.Core.Enumerations;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models.Base;
using Rvnx.CRM.Core.Models.Business;
using Rvnx.CRM.Core.Models.Contact;

namespace Rvnx.CRM.Core.Services;

public class EntityService : IEntityService
{
    private readonly IRepository _repository;

    public EntityService(IRepository repository)
    {
        _repository = repository;
    }

    /// <inheritdoc />
    public async Task<bool> ExistsAsync(EntityType entityType, Guid id)
    {
        return entityType switch
        {
            EntityType.Person => await _repository.ExistsAsync<Contact>(id),
            _ => false
        };
    }

    /// <inheritdoc />
    public async Task<string> GetEntityNameAsync(EntityType entityType, Guid id)
    {
        if (entityType == EntityType.Person)
        {
            List<string> names = await _repository.ListProjectedAsync<Contact, string>(
                c => c.Id == id,
                c => c.FirstName + " " + (c.LastName ?? ""));
            return names.FirstOrDefault()?.Trim() ?? "Unknown Person";
        }
        else if (entityType == EntityType.Company)
        {
            List<string> names = await _repository.ListProjectedAsync<Employer, string>(
                c => c.Id == id,
                c => c.CompanyName);
            return names.FirstOrDefault() ?? "Unknown Company";
        }
        return "Unknown Entity";
    }

    /// <inheritdoc />
    public async Task<bool> IsPartialAsync(EntityType entityType, Guid id)
    {
        if (entityType == EntityType.Person)
        {
            List<bool> partials = await _repository.ListProjectedAsync<Contact, bool>(
                c => c.Id == id,
                c => c.IsPartial);
            return partials.FirstOrDefault();
        }
        return false;
    }
}
