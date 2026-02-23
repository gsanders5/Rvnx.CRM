using Rvnx.CRM.Core.DTOs.Contact;

namespace Rvnx.CRM.Core.Interfaces;

public interface IContactManagementService
{
    /// <summary>
    /// Deletes a contact and its related entities (via Cascade Delete).
    /// </summary>
    /// <param name="contactId">The ID of the contact to delete.</param>
    Task DeleteContactAsync(Guid contactId);

    /// <summary>
    /// Creates a new contact from the provided form data.
    /// Also handles initial related entities like Email, Phone, and Birthday if provided.
    /// </summary>
    /// <param name="contactDto">The data for the new contact.</param>
    /// <returns>A <see cref="ContactOperationResult"/> indicating success or failure.</returns>
    Task<ContactOperationResult> CreateContactAsync(ContactFormDto contactDto);

    /// <summary>
    /// Updates an existing contact, including its profile image if provided.
    /// Updates related entities (Email, Phone, Birthday) or adds them if they don't exist.
    /// </summary>
    /// <param name="id">The ID of the contact to update.</param>
    /// <param name="contactDto">The updated contact data.</param>
    /// <param name="imageStream">An optional stream for the profile image file.</param>
    /// <param name="fileName">The filename of the profile image.</param>
    /// <param name="contentType">The content type of the profile image.</param>
    /// <returns>A <see cref="ContactOperationResult"/> indicating success, failure, or not found.</returns>
    Task<ContactOperationResult> UpdateContactAsync(Guid id, ContactFormDto contactDto, Stream? imageStream, string? fileName, string? contentType);
}
