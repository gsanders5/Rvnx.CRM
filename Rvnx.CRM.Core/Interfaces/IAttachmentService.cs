using Rvnx.CRM.Core.DTOs.Base;
using Rvnx.CRM.Core.Enumerations;

namespace Rvnx.CRM.Core.Interfaces;

public interface IAttachmentService
{
    Task<List<AttachmentDto>> GetByContactAsync(Guid contactId);

    /// <summary>
    /// Uploads a new attachment for a specific entity.
    /// Determines MIME type safely based on file extension and validates content.
    /// </summary>
    /// <param name="entityId">The ID of the entity to attach the file to.</param>
    /// <param name="entityType">The type of the entity.</param>
    /// <param name="content">The raw file content.</param>
    /// <param name="fileName">The original filename.</param>
    /// <returns>A <see cref="AttachmentOperationResult"/> indicating success or failure.</returns>
    Task<AttachmentOperationResult> UploadAttachmentAsync(Guid entityId, EntityType entityType, byte[] content, string fileName);

    /// <summary>
    /// Deletes an attachment and its associated content.
    /// </summary>
    /// <param name="attachmentId">The ID of the attachment to delete.</param>
    /// <returns>A <see cref="AttachmentOperationResult"/> indicating success or failure.</returns>
    Task<AttachmentOperationResult> DeleteAttachmentAsync(Guid attachmentId);

    /// <summary>
    /// Retrieves the binary content of an attachment.
    /// </summary>
    /// <param name="attachmentId">The ID of the attachment.</param>
    /// <returns>A <see cref="AttachmentContentDto"/> containing the bytes and MIME type, or null if not found.</returns>
    Task<AttachmentContentDto?> GetAttachmentContentAsync(Guid attachmentId);

    /// <summary>
    /// Retrieves metadata for an attachment.
    /// </summary>
    /// <param name="attachmentId">The ID of the attachment.</param>
    /// <returns>An <see cref="AttachmentDto"/> with metadata, or null if not found.</returns>
    Task<AttachmentDto?> GetAttachmentAsync(Guid attachmentId);
}