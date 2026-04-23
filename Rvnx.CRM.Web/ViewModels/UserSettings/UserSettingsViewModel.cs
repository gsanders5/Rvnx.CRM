using Rvnx.CRM.Core.DTOs.ApiToken;

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
}
