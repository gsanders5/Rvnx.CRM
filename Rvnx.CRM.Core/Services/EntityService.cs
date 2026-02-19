using Rvnx.CRM.Core.Constants;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models.Base;
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

    public async Task<bool> ExistsAsync(string entityType, Guid id)
    {
        return entityType switch
        {
            EntityTypes.Person => await _repository.ExistsAsync<Contact>(id),
            EntityTypes.Note => await _repository.ExistsAsync<Note>(id),
            EntityTypes.Reminder => await _repository.ExistsAsync<Reminder>(id),
            EntityTypes.SignificantDate => await _repository.ExistsAsync<SignificantDate>(id),
            EntityTypes.Relationship => await _repository.ExistsAsync<Relationship>(id),
            _ => false
        };
    }
}
