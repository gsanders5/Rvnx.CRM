using System.ComponentModel.DataAnnotations;

namespace Rvnx.CRM.Core.Models;

public abstract class CRMBaseEntity
{
    [Key]
    [Required]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(256)]
    public string CreatedBy { get; set; } = string.Empty;

    [Required]
    [MaxLength(256)]
    public string LastChangedBy { get; set; } = string.Empty;

    [Required]
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    [Required]
    public DateTime LastChangedDate { get; set; } = DateTime.UtcNow;
}