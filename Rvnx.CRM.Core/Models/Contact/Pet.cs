using Rvnx.CRM.Core.Models.Base;

namespace Rvnx.CRM.Core.Models.Contact;

public class Pet : BaseEntity
{
    public Guid ContactId { get; set; }

    public virtual Contact Contact { get; set; } = null!;

    public string Name { get; set; } = string.Empty;

    public string? Species { get; set; }

    public string? Breed { get; set; }

    public DateTime? Birthday { get; set; }

    public string? Notes { get; set; }
}