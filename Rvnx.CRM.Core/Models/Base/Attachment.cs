using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rvnx.CRM.Core.Models.Base;

[Table("Attachment")]
public class Attachment : PolymorphicEntity
{
    [Required]
    [MaxLength(100)]
    public string AttachmentType { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string ContentType { get; set; } = string.Empty;

    [MaxLength(255)]
    public string? FileName { get; set; }

    public virtual AttachmentContent? AttachmentContent { get; set; }
}
