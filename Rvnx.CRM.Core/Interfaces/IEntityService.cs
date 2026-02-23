namespace Rvnx.CRM.Core.Interfaces;

public interface IEntityService
{
    /// <summary>
    /// Checks if an entity exists by its type string (e.g., "Person", "Note") and ID.
    /// Useful for validating polymorphic references.
    /// </summary>
    /// <param name="entityType">The string identifier of the entity type.</param>
    /// <param name="id">The ID of the entity.</param>
    /// <returns>True if the entity exists; otherwise, false.</returns>
    Task<bool> ExistsAsync(string entityType, Guid id);
}
