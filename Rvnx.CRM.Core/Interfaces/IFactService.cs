using Rvnx.CRM.Core.DTOs.Contact;
using Rvnx.CRM.Core.Models;
using Rvnx.CRM.Core.Models.Contact;

namespace Rvnx.CRM.Core.Interfaces;

public interface IFactService
{
    Task<List<FactDto>> GetByContactAsync(Guid contactId);

    /// <summary>
    /// Creates a new fact (generic key-value pair) for a contact.
    /// </summary>
    /// <param name="dto">The fact data.</param>
    /// <returns>An <see cref="OperationResult"/> indicating success or failure.</returns>
    Task<OperationResult> CreateAsync(FactFormDto dto);

    /// <summary>
    /// Updates an existing fact.
    /// </summary>
    /// <param name="id">The ID of the fact.</param>
    /// <param name="dto">The updated data.</param>
    /// <returns>An <see cref="OperationResult"/> indicating success or failure.</returns>
    Task<OperationResult> UpdateAsync(Guid id, FactFormDto dto);

    /// <summary>
    /// Deletes a fact.
    /// </summary>
    /// <param name="id">The ID of the fact to delete.</param>
    /// <returns>An <see cref="OperationResult"/> indicating success or failure.</returns>
    Task<OperationResult> DeleteAsync(Guid id);

    /// <summary>
    /// Retrieves the form data for editing a fact.
    /// </summary>
    /// <param name="id">The ID of the fact.</param>
    /// <returns>A <see cref="FactFormDto"/> if found, otherwise null.</returns>
    Task<FactFormDto?> GetFormAsync(Guid id);

    /// <summary>
    /// Initializes a new form DTO for creating a fact linked to a contact.
    /// </summary>
    Task<FactFormDto?> GetFormForCreateAsync(Guid contactId);

    /// <summary>
    /// Retrieves a fact entity by its ID.
    /// </summary>
    /// <param name="id">The ID of the fact.</param>
    /// <returns>The <see cref="Fact"/> entity if found, otherwise null.</returns>
    Task<Fact?> GetByIdAsync(Guid id);
}
