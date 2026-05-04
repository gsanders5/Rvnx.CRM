using Rvnx.CRM.Core.DTOs.Base;
using Rvnx.CRM.Core.DTOs.Contact;

namespace Rvnx.CRM.Core.Interfaces;

public interface ILabelService
{
    /// <summary>
    /// Retrieves all labels available to the current user, ordered by name.
    /// </summary>
    /// <returns>A list of <see cref="LabelDto"/>.</returns>
    Task<List<LabelDto>> GetAllAsync();

    /// <summary>
    /// Retrieves a single label by its ID.
    /// </summary>
    /// <param name="id">The label ID.</param>
    /// <returns>A <see cref="LabelDto"/> if found, otherwise null.</returns>
    Task<LabelDto?> GetByIdAsync(Guid id);

    /// <summary>
    /// Creates a new label.
    /// </summary>
    /// <param name="name">The name of the label (required).</param>
    /// <param name="color">The hex color code (optional).</param>
    /// <returns>A <see cref="LabelOperationResult"/> indicating success or failure.</returns>
    Task<LabelOperationResult> CreateAsync(string name, string? color);

    /// <summary>
    /// Updates an existing label.
    /// </summary>
    /// <param name="id">The ID of the label.</param>
    /// <param name="name">The new name.</param>
    /// <param name="color">The new hex color code.</param>
    /// <returns>A <see cref="LabelOperationResult"/> indicating success or failure.</returns>
    Task<LabelOperationResult> UpdateAsync(Guid id, string name, string? color);

    /// <summary>
    /// Deletes a label.
    /// </summary>
    /// <param name="id">The ID of the label to delete.</param>
    Task DeleteAsync(Guid id);

    /// <summary>
    /// Assigns a label to a contact. If already assigned, does nothing.
    /// </summary>
    /// <param name="contactId">The contact ID.</param>
    /// <param name="labelId">The label ID.</param>
    Task AssignLabelAsync(Guid contactId, Guid labelId);

    /// <summary>
    /// Removes a label from a contact.
    /// </summary>
    /// <param name="contactId">The contact ID.</param>
    /// <param name="labelId">The label ID.</param>
    Task RemoveLabelAsync(Guid contactId, Guid labelId);

    /// <summary>
    /// Assigns a label to multiple contacts. Already-assigned pairs are silently skipped.
    /// </summary>
    /// <param name="contactIds">The contact IDs.</param>
    /// <param name="labelId">The label ID.</param>
    /// <returns>A <see cref="BulkOperationResult"/> with the counts of newly-assigned and already-assigned rows.</returns>
    Task<BulkOperationResult> BulkAssignLabelAsync(IReadOnlyCollection<Guid> contactIds, Guid labelId);

    /// <summary>
    /// Removes a label from multiple contacts. Pairs without an existing assignment are silently skipped.
    /// </summary>
    /// <param name="contactIds">The contact IDs.</param>
    /// <param name="labelId">The label ID.</param>
    /// <returns>A <see cref="BulkOperationResult"/> with the counts of removed and not-assigned rows.</returns>
    Task<BulkOperationResult> BulkRemoveLabelAsync(IReadOnlyCollection<Guid> contactIds, Guid labelId);

    /// <summary>
    /// Retrieves all labels assigned to a specific contact.
    /// </summary>
    /// <param name="contactId">The contact ID.</param>
    /// <returns>A list of <see cref="LabelDto"/>.</returns>
    Task<List<LabelDto>> GetLabelsForContactAsync(Guid contactId);
}
