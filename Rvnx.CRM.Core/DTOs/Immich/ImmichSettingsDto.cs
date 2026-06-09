namespace Rvnx.CRM.Core.DTOs.Immich;

/// <summary>
/// Safe-for-display view of a group's Immich settings. The API key itself is never
/// exposed to the UI; only a masked hint (last four characters) is included.
/// </summary>
public class ImmichSettingsDto
{
    public bool Enabled { get; set; }

    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>Masked representation of the stored API key, e.g. "••••3kfa".</summary>
    public string ApiKeyHint { get; set; } = string.Empty;
}
