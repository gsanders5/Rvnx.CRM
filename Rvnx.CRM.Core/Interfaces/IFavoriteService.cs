namespace Rvnx.CRM.Core.Interfaces;

public interface IFavoriteService
{
    /// <summary>
    /// Toggles the favorite status of a contact for the current user.
    /// </summary>
    /// <param name="contactId">The ID of the contact to toggle.</param>
    /// <returns>True if the contact is now favorited; false if it was unfavorited.</returns>
    Task<bool> ToggleFavoriteAsync(Guid contactId);

    /// <summary>
    /// Returns the set of contact IDs that the current user has favorited.
    /// </summary>
    Task<HashSet<Guid>> GetFavoriteContactIdsAsync();
}
