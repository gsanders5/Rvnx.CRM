using System.ComponentModel.DataAnnotations;
using Rvnx.CRM.Core.Models.Base;

namespace Rvnx.CRM.Core.Models;

public class User : CRMBaseEntity
{
    [Required]
    [MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MaxLength(256)]
    public string SubjectId { get; set; } = string.Empty;

    [MaxLength(256)]
    public string? DisplayName { get; set; }
}
