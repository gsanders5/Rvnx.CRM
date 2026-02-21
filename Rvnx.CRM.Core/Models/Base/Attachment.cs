using Rvnx.CRM.Core.Models.Contact;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rvnx.CRM.Core.Models.Base;

[Table("Attachment")]
public class Attachment : BaseEntity
{
    public Guid? ContactId { get; set; }

    [ForeignKey(nameof(ContactId))]
    public virtual Rvnx.CRM.Core.Models.Contact.Contact? Contact { get; set; }

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
