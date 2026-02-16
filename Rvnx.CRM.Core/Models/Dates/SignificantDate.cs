using Rvnx.CRM.Core.Models.Base;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rvnx.CRM.Core.Models.Dates;

[Table("SignificantDate")]
public class SignificantDate : RemindableEntity
{
    [MaxLength(200)]
    [Display(Name = "Title")]
    public string? Title { get; set; }

    [Display(Name = "Description")]
    public string? Description { get; set; }

    [Required]
    [Display(Name = "Date")]
    [DataType(DataType.Date)]
    public DateTime Date { get; set; }

    public DateTime GetNextOccurrence()
    {
        // Default to yearly if no frequency is set, for backward compatibility with old birthdays
        TimeSpan frequency = EventFrequency;
        if (frequency <= TimeSpan.Zero)
        {
            frequency = TimeSpan.FromDays(365);
        }
        return Rvnx.CRM.Core.Services.DateCalculationService.GetNextOccurrence(Date, frequency);
    }
}
