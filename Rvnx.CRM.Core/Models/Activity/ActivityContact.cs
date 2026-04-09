using Rvnx.CRM.Core.Models.Base;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rvnx.CRM.Core.Models.Activity;

[Table("ActivityContact")]
public class ActivityContact : BaseEntity
{
    public Guid ActivityId { get; set; }
    public virtual Activity Activity { get; set; } = null!;

    public Guid ContactId { get; set; }
    public virtual Contact.Contact Contact { get; set; } = null!;
}