using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models.Base;
using System.ComponentModel.DataAnnotations;

namespace Rvnx.CRM.Core.Models;

public class ApiToken : BaseEntity, IGlobalEntity
{
    [Required]
    [MaxLength(256)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(256)]
    public string TokenHash { get; set; } = string.Empty;

    [Required]
    [MaxLength(8)]
    public string TokenPrefix { get; set; } = string.Empty;

    [Required]
    public new Guid UserId { get; set; }

    [Required]
    public new Guid GroupId { get; set; }

    public DateTime? ExpiresAt { get; set; }

    public DateTime? RevokedAt { get; set; }

    public DateTime LastUsedAt { get; set; }

    public bool IsActive => RevokedAt == null && (ExpiresAt == null || ExpiresAt > DateTime.UtcNow);
}
