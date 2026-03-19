using Rvnx.CRM.Core.Models.Base;

namespace Rvnx.CRM.Core.Models.Contact;

public class Fact : BaseEntity
{
    public Guid? ContactId { get; set; }

    public virtual Contact? Contact { get; set; }

    public string Category { get; set; } = string.Empty;

    public string Value { get; set; } = string.Empty;
}