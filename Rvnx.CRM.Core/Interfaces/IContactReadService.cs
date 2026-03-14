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
}