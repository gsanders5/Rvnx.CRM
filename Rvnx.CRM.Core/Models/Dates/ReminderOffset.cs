using Rvnx.CRM.Core.Models.Base;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rvnx.CRM.Core.Models.Dates;

[Table("ReminderOffset")]
public class ReminderOffset : BaseEntity
{
    public Guid SignificantDateId { get; set; }

    [ForeignKey(nameof(SignificantDateId))]
    public virtual SignificantDate? SignificantDate { get; set; }

    [Display(Name = "Days Before Event")]
    public int DaysBeforeEvent { get; set; }

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;

    public virtual ICollection<ReminderLog> ReminderLogs { get; set; } = [];
}
