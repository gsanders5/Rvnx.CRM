namespace Rvnx.CRM.Core.DTOs.Base;

public class AttachmentContentDto
{
    public Guid Id { get; set; }
    public byte[] Content { get; set; } = [];
    public string ContentType { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public DateTime LastChangedDate { get; set; }
}
