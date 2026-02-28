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
            Contact? p = await _repository.GetByIdAsync<Contact>(id);
            return p?.FullName ?? "Unknown Person";
        }
        else if (entityType == EntityTypes.Company)
        {
            Employer? c = await _repository.GetByIdAsync<Employer>(id);
            return c?.CompanyName ?? "Unknown Company";
        }
        return "Unknown Entity";
    }

    /// <inheritdoc />
    public async Task<bool> IsPartialAsync(string entityType, Guid id)
    {
        if (entityType == EntityTypes.Person)
        {
            Contact? c = await _repository.GetByIdAsync<Contact>(id);
            return c?.IsPartial == true;
        }
        return false;
    }
}
