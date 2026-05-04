namespace Rvnx.CRM.Core.Interfaces;

public interface IContactLookupService
{
    /// <summary>
    /// Checks if a contact exists by its ID.
    /// </summary>
    Task<bool> ExistsAsync(Guid id);

    /// <summary>
    /// Gets the display name of the contact, or "Unknown Person" if not found.
    /// </summary>
    Task<string> GetContactNameAsync(Guid id);

    /// <summary>
    /// Checks if the contact is partial (created via a relationship but not fully populated).
    /// </summary>
    Task<bool> IsPartialAsync(Guid id);

    /// <summary>
    /// Returns the subset of <paramref name="ids"/> that are partial contacts, in a single query.
    /// </summary>
    Task<HashSet<Guid>> GetPartialContactIdsAsync(IEnumerable<Guid> ids);
}
