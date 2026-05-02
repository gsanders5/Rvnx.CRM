using Rvnx.CRM.Core.DTOs.Contact;

namespace Rvnx.CRM.Core.Interfaces;

public interface IContactReadService
{
    /// <summary>
    /// Retrieves a list of contacts for the index view, optionally including hidden contacts.
    /// Related data (like profile images and labels) is stitched in memory.
    /// </summary>
    /// <param name="showHidden">If true, includes contacts marked as hidden.</param>
    /// <returns>A list of <see cref="ContactDto"/> objects.</returns>
    Task<List<ContactDto>> GetIndexDataAsync(bool showHidden);

    /// <summary>
    /// Retrieves detailed information for a specific contact, including relationships and related entities.
    /// </summary>
    /// <param name="id">The unique identifier of the contact.</param>
    /// <returns>A <see cref="ContactDetailDto"/> if found; otherwise, null.</returns>
    Task<ContactDetailDto?> GetContactDetailsAsync(Guid id);

    /// <summary>
    /// Retrieves the form data for editing a contact.
    /// </summary>
    /// <param name="id">The unique identifier of the contact.</param>
    /// <returns>A <see cref="ContactFormDto"/> containing the contact's editable properties if found; otherwise, null.</returns>
    Task<ContactFormDto?> GetContactFormAsync(Guid id);

    /// <summary>
    /// Checks if a contact exists and is not a partial contact.
    /// </summary>
    /// <param name="id">The unique identifier of the contact.</param>
    /// <returns>True if the contact exists and is a full contact; otherwise, false.</returns>
    Task<bool> ContactExistsAsync(Guid id);

    /// <summary>
    /// Returns true if the contact has at least one relationship (in either direction).
    /// </summary>
    Task<bool> HasRelationshipsAsync(Guid id);

    /// <summary>
    /// Retrieves a lightweight list of contact Id and FullName pairs for use in select lists.
    /// </summary>
    /// <param name="excludeDeceased">
    /// When true, omits contacts marked as deceased — appropriate for forward-looking pickers
    /// (e.g. logging an activity, scheduling a task). Defaults to false so historical / structural
    /// surfaces (merge, relationships) keep deceased contacts visible.
    /// </param>
    /// <param name="alwaysIncludeIds">
    /// Optional set of contact IDs that must remain in the list even when
    /// <paramref name="excludeDeceased"/> is true. Used on edit forms so an already-attached
    /// deceased contact is still selectable.
    /// </param>
    Task<List<(Guid Id, string FullName)>> GetContactNamesAsync(
        bool excludeDeceased = false,
        IEnumerable<Guid>? alwaysIncludeIds = null);
}
