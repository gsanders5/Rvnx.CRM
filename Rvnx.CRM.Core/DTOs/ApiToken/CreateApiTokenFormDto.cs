using System.ComponentModel.DataAnnotations;

namespace Rvnx.CRM.Core.DTOs.ApiToken;

public class CreateApiTokenFormDto
{
    [Required]
    [MaxLength(256)]
    public string Name { get; set; } = string.Empty;

    public DateTime? ExpiresAt { get; set; }
}
