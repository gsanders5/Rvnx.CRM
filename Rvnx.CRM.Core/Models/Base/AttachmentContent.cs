using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rvnx.CRM.Core.Models.Base;

[Table("AttachmentContent")]
public class AttachmentContent : BaseEntity
{
    [Required]
    public byte[] Content { get; set; } = [];

    [Required]
    public Guid AttachmentId { get; set; }

    [ForeignKey(nameof(AttachmentId))]
    public virtual Attachment? Attachment { get; set; }
}
