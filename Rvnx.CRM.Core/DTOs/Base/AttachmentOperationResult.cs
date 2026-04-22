namespace Rvnx.CRM.Core.DTOs.Base;

public class AttachmentOperationResult : EntityOperationResult
{
    public Guid? AttachmentId { get; set; }

    public static AttachmentOperationResult Failure(params string[] errors)
    {
        return new AttachmentOperationResult { Success = false, Errors = errors.ToList() };
    }

    public static AttachmentOperationResult Ok(Guid attachmentId)
    {
        return new AttachmentOperationResult { Success = true, AttachmentId = attachmentId };
    }

    public static AttachmentOperationResult NotFound(string error = "Attachment not found.")
    {
        return new AttachmentOperationResult { Success = false, IsNotFound = true, Errors = { error } };
    }
}
