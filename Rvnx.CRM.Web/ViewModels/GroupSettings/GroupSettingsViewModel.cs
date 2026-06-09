using Rvnx.CRM.Core.DTOs.Immich;

namespace Rvnx.CRM.Web.ViewModels.GroupSettings;

public class GroupSettingsViewModel
{
    /// <summary>Stored Immich settings for the group; null when Immich has never been configured.</summary>
    public ImmichSettingsDto? Immich { get; set; }

    public ImmichSettingsFormDto ImmichForm { get; set; } = new();

    public bool HasImmichSettings => Immich != null;

    public string? StatusMessage { get; set; }
}
