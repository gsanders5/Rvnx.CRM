using Rvnx.CRM.Core.Models.Base;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rvnx.CRM.Core.Models.Dates;

[Table("SignificantDate")]
public class SignificantDate : BaseEntity
{
    public Guid? ContactId { get; set; }

    [ForeignKey(nameof(ContactId))]
    public virtual Rvnx.CRM.Core.Models.Contact.Contact? Contact { get; set; }

    [MaxLength(200)]
    [Display(Name = "Title")]
    public string? Title { get; set; }

    [Display(Name = "Description")]
    public string? Description { get; set; }

    [Required]
    [Display(Name = "Date")]
    [DataType(DataType.Date)]
    public DateOnly EventDate { get; set; }

    [Display(Name = "Recurrence Type")]
    public Rvnx.CRM.Core.Enumerations.RecurrenceType RecurrenceType { get; set; } = Rvnx.CRM.Core.Enumerations.RecurrenceType.None;

    [Display(Name = "Custom Interval Days")]
    public int? CustomIntervalDays { get; set; }

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;

    public virtual ICollection<ReminderOffset> ReminderOffsets { get; set; } = [];

    public DateOnly GetNextOccurrence()
    {
        return Rvnx.CRM.Core.Services.DateCalculationService.GetNextOccurrence(this, DateOnly.FromDateTime(DateTime.Today));
    }
}