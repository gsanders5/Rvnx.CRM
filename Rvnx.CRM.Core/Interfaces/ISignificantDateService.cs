using Rvnx.CRM.Core.DTOs.Dates;
using Rvnx.CRM.Core.Models;
using Rvnx.CRM.Core.Models.Dates;

namespace Rvnx.CRM.Core.Interfaces;

public interface ISignificantDateService
{
    Task<List<SignificantDateDto>> GetByContactAsync(Guid contactId);

    /// <summary>
    /// Creates a new significant date (e.g., anniversary) for a contact.
    /// </summary>
    /// <param name="dto">The date data.</param>
    /// <returns>An <see cref="OperationResult"/> indicating success or failure.</returns>
    Task<OperationResult> CreateAsync(SignificantDateDto dto);

    Task<OperationResult> AddReminderOffsetAsync(Guid significantDateId, int daysBeforeEvent);

    Task<OperationResult> DeleteReminderOffsetAsync(Guid offsetId);

    /// <summary>
    /// Updates an existing significant date.
    /// </summary>
    /// <param name="id">The ID of the significant date.</param>
    /// <param name="dto">The updated data.</param>
    /// <returns>An <see cref="OperationResult"/> indicating success or failure.</returns>
    Task<OperationResult> UpdateAsync(Guid id, SignificantDateDto dto);

    /// <summary>
    /// Deletes a significant date.
    /// </summary>
    /// <param name="id">The ID of the date to delete.</param>
    /// <returns>An <see cref="OperationResult"/> indicating success or failure.</returns>
    Task<OperationResult> DeleteAsync(Guid id);

    /// <summary>
    /// Retrieves the DTO for a significant date.
    /// </summary>
    /// <param name="id">The ID of the date.</param>
    /// <returns>A <see cref="SignificantDateDto"/> if found, otherwise null.</returns>
    Task<SignificantDateDto?> GetDtoAsync(Guid id);

    /// <summary>
    /// Retrieves a significant date entity by its ID.
    /// </summary>
    /// <param name="id">The ID of the date.</param>
    /// <returns>The <see cref="SignificantDate"/> entity if found, otherwise null.</returns>
    Task<SignificantDate?> GetByIdAsync(Guid id);
}
