using Rvnx.CRM.Core.Constants;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models.Base;
using Rvnx.CRM.Core.Models.Business;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Core.Models.Dates;

namespace Rvnx.CRM.Core.Services;

public class EntityService : IEntityService
{
    private readonly IRepository _repository;

    public EntityService(IRepository repository)
    {
        _repository = repository;
    }

    /// <inheritdoc />
    public async Task<bool> ExistsAsync(string entityType, Guid id)
    {
        return entityType switch
        {
            EntityTypes.Person => await _repository.ExistsAsync<Contact>(id),
            EntityTypes.Note => await _repository.ExistsAsync<Note>(id),
            EntityTypes.SignificantDate => await _repository.ExistsAsync<SignificantDate>(id),
            EntityTypes.Relationship => await _repository.ExistsAsync<Relationship>(id),
            _ => false
        };
    }

    /// <inheritdoc />
    public async Task<string> GetEntityNameAsync(string entityType, Guid id)
    {
        if (entityType == EntityTypes.Person)
        {
            // Optimization: Fetch only the names instead of the entire Contact entity
            List<string> names = await _repository.ListProjectedAsync<Contact, string>(
                c => c.Id == id,
                c => c.FirstName + " " + (c.LastName ?? ""));
            return names.FirstOrDefault()?.Trim() ?? "Unknown Person";
        }
        else if (entityType == EntityTypes.Company)
        {
            // Optimization: Fetch only the name instead of the entire Employer entity
            List<string> names = await _repository.ListProjectedAsync<Employer, string>(
                c => c.Id == id,
                c => c.CompanyName);
            return names.FirstOrDefault() ?? "Unknown Company";
        }
        return "Unknown Entity";
    }

    /// <inheritdoc />
    public async Task<bool> IsPartialAsync(string entityType, Guid id)
    {
        if (entityType == EntityTypes.Person)
        {
            // Optimization: Fetch only the IsPartial flag instead of the entire Contact entity
            List<bool> partials = await _repository.ListProjectedAsync<Contact, bool>(
                c => c.Id == id,
                c => c.IsPartial);
            return partials.FirstOrDefault();
        }
        return false;
    }
}