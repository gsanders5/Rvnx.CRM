using Rvnx.CRM.Core.Models.Base;
using System.ComponentModel.DataAnnotations;

namespace Rvnx.CRM.Core.Models.Contact;

public class Fact : BaseEntity
{
    public Guid? ContactId { get; set; }

    public virtual Contact? Contact { get; set; }

    [Required]
    [MaxLength(100)]
    public string Category { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string Value { get; set; } = string.Empty;
}