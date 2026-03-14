using Rvnx.CRM.Core.Models.Base;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rvnx.CRM.Core.Models.Dates;

[Table("ReminderLog")]
public class ReminderLog : BaseEntity
{
    public Guid ReminderOffsetId { get; set; }

    [ForeignKey(nameof(ReminderOffsetId))]
    public virtual ReminderOffset? ReminderOffset { get; set; }

    [Display(Name = "Occurrence Date")]
    public DateOnly OccurrenceDate { get; set; }

    [Display(Name = "Scheduled For")]
    public DateOnly ScheduledFor { get; set; }

    [Display(Name = "Sent At")]
    public DateTime? SentAt { get; set; }

    public bool Success { get; set; }

    [MaxLength(200)]
    [Display(Name = "Email Address")]
    public string? EmailAddress { get; set; }

    [Display(Name = "Error Message")]
    public string? ErrorMessage { get; set; }
}