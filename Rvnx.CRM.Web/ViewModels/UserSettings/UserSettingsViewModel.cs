using Rvnx.CRM.Core.DTOs.ApiToken;
using Rvnx.CRM.Core.DTOs.Immich;

namespace Rvnx.CRM.Web.ViewModels.UserSettings;

public class UserSettingsViewModel
{
    public string? DisplayName { get; set; }
    public string? Email { get; set; }
    public IReadOnlyList<ApiTokenDto> Tokens { get; set; } = [];
    public string? NewlyCreatedRawToken { get; set; }
    public string? NewlyCreatedTokenName { get; set; }
    public bool ShowDangerZone { get; set; }
    public bool IsDevelopment { get; set; }
    public bool ShowDevOperations => ShowDangerZone && IsDevelopment;
    public string? DangerZoneMessage { get; set; }
    public string? DangerZoneError { get; set; }

    /// <summary>Whether this server permits Immich integration at all (global config flag).</summary>
    public bool ImmichServerEnabled { get; set; }

    /// <summary>Stored Immich settings for the group; null when Immich has never been configured.</summary>
    public ImmichSettingsDto? Immich { get; set; }

    public ImmichSettingsFormDto ImmichForm { get; set; } = new();

    public bool HasImmichSettings => Immich != null;

    public string? ImmichStatusMessage { get; set; }
}
