using Rvnx.CRM.Core.DTOs.Contact;

namespace Rvnx.CRM.Core.Interfaces;

public interface ISelfContactService
{
    /// <summary>
    /// Retrieves the ID of the contact entity linked to the currently authenticated user.
    /// </summary>
    /// <returns>The ID of the self contact, or null if not yet created.</returns>
    Task<Guid?> GetSelfContactIdAsync();

    /// <summary>
    /// Prepares a contact form DTO pre-filled with data from the user's claims (Name, Email).
    /// </summary>
    /// <returns>A <see cref="ContactFormDto"/> or null if the user is not authenticated.</returns>
    Task<ContactFormDto?> GetSelfContactFormAsync();

    /// <summary>
    /// Creates a new contact entity representing the current user and links it to their User record.
    /// </summary>
    /// <param name="contactDto">The contact data.</param>
    /// <returns>A <see cref="ContactOperationResult"/> indicating success or failure.</returns>
    Task<ContactOperationResult> CreateSelfContactAsync(ContactFormDto contactDto);
}
