using Rvnx.CRM.Core.DTOs.Base;

namespace Rvnx.CRM.Core.DTOs.Contact;

public class ActivityDto : BaseDto
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime ActivityDate { get; set; }
    public string? ActivityType { get; set; }
    public string? Location { get; set; }
    public Guid EntityId { get; set; }
    public List<Guid> ContactIds { get; set; } = [];
    public List<string> ContactNames { get; set; } = [];

    /// <summary>
    /// Per-participant deceased flags, parallel to <see cref="ContactIds"/> and <see cref="ContactNames"/>.
    /// Populated by <see cref="Rvnx.CRM.Core.Services.ContactReadService"/> so the Activities card can render
    /// a deceased indicator next to a participant's name without re-querying.
    /// </summary>
    public List<bool> ContactIsDeceased { get; set; } = [];
}
