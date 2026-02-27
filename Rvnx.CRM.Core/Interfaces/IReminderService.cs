using Rvnx.CRM.Core.DTOs.Dates;
using Rvnx.CRM.Core.Models;

namespace Rvnx.CRM.Core.Interfaces;

public interface IReminderService
{
    /// <summary>
    /// Creates a new reminder for an entity.
    /// </summary>
    /// <param name="dto">The reminder data.</param>
    /// <returns>An <see cref="OperationResult"/> indicating success or failure.</returns>
    Task<OperationResult> CreateAsync(ReminderFormViewModel dto);

    /// <summary>
    /// Updates an existing reminder.
    /// </summary>
    /// <param name="id">The ID of the reminder.</param>
    /// <param name="dto">The updated data.</param>
    /// <returns>An <see cref="OperationResult"/> indicating success or failure.</returns>
    Task<OperationResult> UpdateAsync(Guid id, ReminderFormViewModel dto);

    /// <summary>
    /// Deletes a reminder.
    /// </summary>
    /// <param name="id">The ID of the reminder to delete.</param>
    /// <returns>An <see cref="OperationResult"/> indicating success or failure.</returns>
    Task<OperationResult> DeleteAsync(Guid id);

    /// <summary>
    /// Retrieves the form data for editing a reminder.
    /// </summary>
    /// <param name="id">The ID of the reminder.</param>
    /// <returns>A <see cref="ReminderFormViewModel"/> if found, otherwise null.</returns>
    Task<ReminderFormViewModel?> GetFormAsync(Guid id);

    /// <summary>
    /// Initializes a new form DTO for creating a reminder, linked to a specific entity.
    /// </summary>
    /// <param name="entityId">The ID of the parent entity.</param>
    /// <param name="entityType">The type of the parent entity.</param>
    /// <returns>A pre-populated <see cref="ReminderFormViewModel"/>.</returns>
    Task<ReminderFormViewModel?> GetFormForCreateAsync(Guid entityId, string entityType);

    /// <summary>
    /// Retrieves a reminder DTO by its ID.
    /// </summary>
    /// <param name="id">The ID of the reminder.</param>
    /// <returns>A <see cref="ReminderDto"/> if found, otherwise null.</returns>
    Task<ReminderDto?> GetDtoAsync(Guid id);
}
