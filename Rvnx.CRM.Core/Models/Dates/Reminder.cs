using Rvnx.CRM.Core.Models.Base;
using Rvnx.CRM.Core.Models.Contact;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rvnx.CRM.Core.Models.Dates;

[Table("Reminder")]
public class Reminder : CRMBaseEntity
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

    [Display(Name = "Person ID")]
    public Guid? PersonId { get; set; }

    [ForeignKey(nameof(PersonId))]
    public virtual Person? Person { get; set; }
}
