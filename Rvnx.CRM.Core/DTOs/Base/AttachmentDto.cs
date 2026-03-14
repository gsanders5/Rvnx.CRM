namespace Rvnx.CRM.Core.DTOs.Base
{
    public class AttachmentDto
    {
        public Guid Id { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public string AttachmentType { get; set; } = string.Empty; // e.g. "ProfileImage"
        public Guid EntityId { get; set; }
        public string EntityType { get; set; } = string.Empty;
        // Content might be loaded separately or included if small.
        // For lists, we usually don't include content.
    }
}