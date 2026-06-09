using System.ComponentModel.DataAnnotations;

namespace Rvnx.CRM.Web.ViewModels.UserSettings;

public class ImmichSettingsFormDto
{
    public bool Enabled { get; set; }

    [Required(ErrorMessage = "Server URL is required.")]
    [MaxLength(2048)]
    [Display(Name = "Server URL")]
    public string BaseUrl { get; set; } = string.Empty;

    // Optional on update (blank keeps the stored key); the service enforces presence on first save.
    [MaxLength(512)]
    [Display(Name = "API Key")]
    public string? ApiKey { get; set; }
}
