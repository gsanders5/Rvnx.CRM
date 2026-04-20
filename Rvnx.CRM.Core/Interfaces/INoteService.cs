using Rvnx.CRM.Core.DTOs.Base;
using Rvnx.CRM.Core.Enumerations;
using Rvnx.CRM.Core.Models;
using Rvnx.CRM.Core.Models.Base;

namespace Rvnx.CRM.Core.Interfaces;

public interface INoteService
{
    Task<List<NoteDto>> GetByContactAsync(Guid contactId);

    /// <summary>
    /// Creates a new note for an entity.
    /// </summary>
    /// <param name="dto">The note data.</param>
    /// <returns>An <see cref="OperationResult"/> indicating success or failure.</returns>
    Task<OperationResult> CreateAsync(NoteFormViewModel dto);

    /// <summary>
    /// Updates an existing note.
    /// </summary>
    /// <param name="id">The ID of the note.</param>
    /// <param name="dto">The updated data.</param>
    /// <returns>An <see cref="OperationResult"/> indicating success or failure.</returns>
    Task<OperationResult> UpdateAsync(Guid id, NoteFormViewModel dto);

    /// <summary>
    /// Deletes a note.
    /// </summary>
    /// <param name="id">The ID of the note to delete.</param>
    /// <returns>An <see cref="OperationResult"/> indicating success or failure.</returns>
    Task<OperationResult> DeleteAsync(Guid id);

    /// <summary>
    /// Retrieves the form data for editing a note.
    /// </summary>
    /// <param name="id">The ID of the note.</param>
    /// <returns>A <see cref="NoteFormViewModel"/> if found, otherwise null.</returns>
    Task<NoteFormViewModel?> GetFormAsync(Guid id);

    /// <summary>
    /// Initializes a new form DTO for creating a note, linked to a specific entity.
    /// </summary>
    /// <param name="entityId">The ID of the parent entity.</param>
    /// <param name="entityType">The type of the parent entity.</param>
    /// <returns>A pre-populated <see cref="NoteFormViewModel"/>.</returns>
    Task<NoteFormViewModel?> GetFormForCreateAsync(Guid entityId, EntityType entityType);

    /// <summary>
    /// Retrieves a note entity by its ID.
    /// </summary>
    /// <param name="id">The ID of the note.</param>
    /// <returns>The <see cref="Note"/> entity if found, otherwise null.</returns>
    Task<Note?> GetByIdAsync(Guid id);
}