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

    /// <summary>
    /// Gets the display name of the entity based on its type and ID.
    /// </summary>
    /// <param name="entityType">The string identifier of the entity type.</param>
    /// <param name="id">The ID of the entity.</param>
    /// <returns>The display name of the entity, or "Unknown Entity" if not found.</returns>
    Task<string> GetEntityNameAsync(string entityType, Guid id);

    /// <summary>
    /// Checks if the entity is a partial contact (i.e., created via a relationship but not fully populated).
    /// </summary>
    /// <param name="entityType">The string identifier of the entity type.</param>
    /// <param name="id">The ID of the entity.</param>
    /// <returns>True if the entity is a partial contact; otherwise, false.</returns>
    Task<bool> IsPartialAsync(string entityType, Guid id);
}
