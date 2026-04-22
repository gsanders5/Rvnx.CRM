using System.ComponentModel.DataAnnotations;

namespace Rvnx.CRM.Core.Models.Base;

public abstract class BaseEntity
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(256)]
    public string CreatedBy { get; set; } = "System";

    [Required]
    [MaxLength(256)]
    public string LastChangedBy { get; set; } = "System";

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public DateTime LastChangedDate { get; set; } = DateTime.UtcNow;

    public Guid? UserId { get; set; }

    public Guid? GroupId { get; set; }
}
