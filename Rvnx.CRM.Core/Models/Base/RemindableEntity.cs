using System.ComponentModel.DataAnnotations.Schema;

namespace Rvnx.CRM.Core.Models.Base;

public abstract class RemindableEntity : BaseEntity
{
    public Guid? ContactId { get; set; }

    [ForeignKey(nameof(ContactId))]
    public virtual Rvnx.CRM.Core.Models.Contact.Contact? Contact { get; set; }

    public bool RemindMe { get; set; }
    public DateTime? ReminderSent { get; set; }
    public TimeSpan EventFrequency { get; set; } = TimeSpan.FromDays(365);
}
