using System.ComponentModel.DataAnnotations;
using Rvnx.CRM.Core.Models.Base;

namespace Rvnx.CRM.Core.Models;

/// <summary>
/// Immich server connection settings for a user group. One row per group (unique GroupId index);
/// every member of the group shares the same Immich server and API key.
/// </summary>
public class GroupImmichSettings : BaseEntity
{
    public bool Enabled { get; set; }

    /// <summary>API base URL including the /api suffix, e.g. "https://immich.example.com/api".</summary>
    [Required]
    [MaxLength(2048)]
    public string BaseUrl { get; set; } = string.Empty;

    [Required]
    [MaxLength(512)]
    public string ApiKey { get; set; } = string.Empty;
}
