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
        DateTime baseDate = DueDate;

        if (IsCompleted && EventFrequency > TimeSpan.Zero)
        {
             baseDate = baseDate.Add(EventFrequency);
        }

        return Rvnx.CRM.Core.Services.DateCalculationService.GetNextOccurrence(baseDate, EventFrequency);
    }
}
