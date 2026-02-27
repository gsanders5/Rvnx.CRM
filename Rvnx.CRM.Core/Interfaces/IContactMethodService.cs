using Rvnx.CRM.Core.DTOs.Contact;
using Rvnx.CRM.Core.Models;
using Rvnx.CRM.Core.Models.Contact;

namespace Rvnx.CRM.Core.Interfaces;

public interface IContactMethodService
{
    /// <summary>
    /// Creates a new contact method (e.g., email, phone) for a contact.
    /// </summary>
    /// <param name="dto">The data for the new contact method.</param>
    /// <returns>An <see cref="OperationResult"/> indicating success or failure.</returns>
    Task<OperationResult> CreateAsync(ContactMethodFormDto dto);

    /// <summary>
    /// Updates an existing contact method.
    /// </summary>
    /// <param name="id">The ID of the contact method to update.</param>
    /// <param name="dto">The updated data.</param>
    /// <returns>An <see cref="OperationResult"/> indicating success or failure.</returns>
    Task<OperationResult> UpdateAsync(Guid id, ContactMethodFormDto dto);

    /// <summary>
    /// Deletes a contact method.
    /// </summary>
    /// <param name="id">The ID of the contact method to delete.</param>
    /// <returns>An <see cref="OperationResult"/> indicating success or failure.</returns>
    Task<OperationResult> DeleteAsync(Guid id);

    /// <summary>
    /// Retrieves the form data for editing a contact method.
    /// </summary>
    /// <param name="id">The ID of the contact method.</param>
    /// <returns>A <see cref="ContactMethodFormDto"/> if found, otherwise null.</returns>
    Task<ContactMethodFormDto?> GetFormAsync(Guid id);

    /// <summary>
    /// Initializes a new form DTO for creating a contact method, linked to a specific entity.
    /// </summary>
    /// <param name="entityId">The ID of the parent entity (e.g., Contact).</param>
    /// <param name="entityType">The type of the parent entity.</param>
    /// <returns>A pre-populated <see cref="ContactMethodFormDto"/>.</returns>
    Task<ContactMethodFormDto?> GetFormForCreateAsync(Guid entityId, string entityType);

    /// <summary>
    /// Retrieves a contact method entity by its ID.
    /// </summary>
    /// <param name="id">The ID of the contact method.</param>
    /// <returns>The <see cref="ContactMethod"/> entity if found, otherwise null.</returns>
    Task<ContactMethod?> GetByIdAsync(Guid id);
}
