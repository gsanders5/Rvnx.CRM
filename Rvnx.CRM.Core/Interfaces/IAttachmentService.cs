using Rvnx.CRM.Core.DTOs.Base;

namespace Rvnx.CRM.Core.Interfaces;

public interface IAttachmentService
{
    Task<AttachmentOperationResult> UploadAttachmentAsync(Guid entityId, string entityType, byte[] content, string fileName, string contentType);
    Task<AttachmentOperationResult> DeleteAttachmentAsync(Guid attachmentId);
    Task<AttachmentContentDto?> GetAttachmentContentAsync(Guid attachmentId);
    Task<AttachmentDto?> GetAttachmentAsync(Guid attachmentId);
}
