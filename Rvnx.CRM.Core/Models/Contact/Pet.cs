using Rvnx.CRM.Core.Models.Base;
using System.ComponentModel.DataAnnotations;

namespace Rvnx.CRM.Core.Models.Contact;

public class Pet : BaseEntity
{
    [Required]
    public Guid ContactId { get; set; }

    public virtual Contact Contact { get; set; } = null!;

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? Species { get; set; }

    [MaxLength(100)]
    public string? Breed { get; set; }

    public DateTime? Birthday { get; set; }

    public string? Notes { get; set; }
}