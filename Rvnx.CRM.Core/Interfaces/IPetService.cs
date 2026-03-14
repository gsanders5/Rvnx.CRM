using Rvnx.CRM.Core.DTOs.Contact;
using Rvnx.CRM.Core.Models;
using Rvnx.CRM.Core.Models.Contact;

namespace Rvnx.CRM.Core.Interfaces;

public interface IPetService
{
    /// <summary>
    /// Creates a new pet entry for a contact.
    /// </summary>
    /// <param name="dto">The pet data.</param>
    /// <returns>An <see cref="OperationResult"/> indicating success or failure.</returns>
    Task<OperationResult> CreateAsync(PetFormDto dto);

    /// <summary>
    /// Updates an existing pet entry.
    /// </summary>
    /// <param name="id">The ID of the pet.</param>
    /// <param name="dto">The updated data.</param>
    /// <returns>An <see cref="OperationResult"/> indicating success or failure.</returns>
    Task<OperationResult> UpdateAsync(Guid id, PetFormDto dto);

    /// <summary>
    /// Deletes a pet entry.
    /// </summary>
    /// <param name="id">The ID of the pet to delete.</param>
    /// <returns>An <see cref="OperationResult"/> indicating success or failure.</returns>
    Task<OperationResult> DeleteAsync(Guid id);

    /// <summary>
    /// Retrieves the form data for editing a pet.
    /// </summary>
    /// <param name="id">The ID of the pet.</param>
    /// <returns>A <see cref="PetFormDto"/> if found, otherwise null.</returns>
    Task<PetFormDto?> GetFormAsync(Guid id);

    /// <summary>
    /// Initializes a new form DTO for adding a pet to a contact.
    /// </summary>
    /// <param name="entityId">The ID of the contact (owner).</param>
    /// <returns>A pre-populated <see cref="PetFormDto"/>.</returns>
    Task<PetFormDto?> GetFormForCreateAsync(Guid entityId);

    /// <summary>
    /// Retrieves a pet entity by its ID.
    /// </summary>
    /// <param name="id">The ID of the pet.</param>
    /// <returns>The <see cref="Pet"/> entity if found, otherwise null.</returns>
    Task<Pet?> GetByIdAsync(Guid id);
}