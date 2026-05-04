
namespace Rvnx.CRM.Core.DTOs.Base;

public class AttachmentDto
{
    public Guid Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public string AttachmentType { get; set; } = string.Empty;
    public Guid ContactId { get; set; }
}
