using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models.Contact;

namespace Rvnx.CRM.Core.Services;

public class ContactLookupService : IContactLookupService
{
    private readonly IRepository _repository;

    public ContactLookupService(IRepository repository)
    {
        _repository = repository;
    }

    /// <inheritdoc />
    public async Task<bool> ExistsAsync(Guid id)
    {
        return await _repository.ExistsAsync<Contact>(id);
    }

    /// <inheritdoc />
    public async Task<string> GetContactNameAsync(Guid id)
    {
        List<string> names = await _repository.ListProjectedAsync<Contact, string>(
            c => c.Id == id,
            c => c.FirstName + " " + (c.LastName ?? ""));
        return names.FirstOrDefault()?.Trim() ?? "Unknown Person";
    }

    /// <inheritdoc />
    public async Task<bool> IsPartialAsync(Guid id)
    {
        List<bool> partials = await _repository.ListProjectedAsync<Contact, bool>(
            c => c.Id == id,
            c => c.IsPartial);
        return partials.FirstOrDefault();
    }

    /// <inheritdoc />
    public async Task<HashSet<Guid>> GetPartialContactIdsAsync(IEnumerable<Guid> ids)
    {
        HashSet<Guid> idSet = [.. ids];
        if (idSet.Count == 0)
        {
            return [];
        }

        List<Guid> partialIds = await _repository.ListProjectedAsync<Contact, Guid>(
            c => idSet.Contains(c.Id) && c.IsPartial,
            c => c.Id);
        return [.. partialIds];
    }
}
