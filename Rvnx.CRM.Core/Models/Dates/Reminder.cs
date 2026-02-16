using Rvnx.CRM.Core.Models.Base;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rvnx.CRM.Core.Models.Dates;

[Table("Reminder")]
public class Reminder : RemindableEntity
{
    [Required]
    [MaxLength(200)]
    [Display(Name = "Title")]
    public string Title { get; set; } = string.Empty;

    [Display(Name = "Description")]
    public string? Description { get; set; }

    [Required]
    [Display(Name = "Due Date")]
    public DateTime DueDate { get; set; }

    [Display(Name = "Is Completed")]
    public bool IsCompleted { get; set; } = false;

    public DateTime GetNextOccurrence()
    {
        // For reminders, if completed, start from next cycle (or logic in Controller?)
        // The original controller logic:
        // if (reminder.IsCompleted) nextDate = nextDate.Add(reminder.EventFrequency);
        // then loop.

        // However, our Service treats 'originalDate' as the base.
        // If we pass 'DueDate' as originalDate, the Service will return the *next* valid date >= Today.

        // If the reminder is completed, the 'DueDate' is technically 'done'.
        // So we might want to pass 'DueDate + Frequency' as the start if it is completed?
        // But the service handles "finding the next occurrence".

        // Let's defer to the service logic: finding the next valid occurrence based on the pattern.
        // However, for Reminders, 'IsCompleted' specifically means "this instance is done, give me the next one".
        // If DueDate is Today and IsCompleted=true, we want Tomorrow (or next cycle).
        // If DueDate is Today and IsCompleted=false, we want Today.

        // The Service returns 'today' if date <= today.

        DateTime baseDate = DueDate;
        if (IsCompleted && EventFrequency > TimeSpan.Zero)
        {
             baseDate = baseDate.Add(EventFrequency);
        }

        return Rvnx.CRM.Core.Services.DateCalculationService.GetNextOccurrence(baseDate, EventFrequency);
    }
}
