using Rvnx.CRM.Core.DTOs.Contact;
using System.Security.Claims;

namespace Rvnx.CRM.Core.Interfaces;

public interface ISelfContactService
{
    /// <summary>
    /// Retrieves the ID of the contact entity linked to the currently authenticated user.
    /// </summary>
    /// <param name="user">The current user principal.</param>
    /// <returns>The ID of the self contact, or null if not yet created.</returns>
    Task<Guid?> GetSelfContactIdAsync(ClaimsPrincipal user);

    /// <summary>
    /// Prepares a contact form DTO pre-filled with data from the user's claims (Name, Email).
    /// </summary>
    /// <param name="user">The current user principal.</param>
    /// <returns>A <see cref="ContactFormDto"/> or null if the user is not authenticated.</returns>
    Task<ContactFormDto?> GetSelfContactFormAsync(ClaimsPrincipal user);

    /// <summary>
    /// Creates a new contact entity representing the current user and links it to their User record.
    /// </summary>
    /// <param name="user">The current user principal.</param>
    /// <param name="contactDto">The contact data.</param>
    /// <returns>A <see cref="ContactOperationResult"/> indicating success or failure.</returns>
    Task<ContactOperationResult> CreateSelfContactAsync(ClaimsPrincipal user, ContactFormDto contactDto);
}