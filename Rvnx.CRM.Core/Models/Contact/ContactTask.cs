using Rvnx.CRM.Core.Models.Base;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rvnx.CRM.Core.Models.Contact;

[Table("ContactTask")]
public class ContactTask : BaseEntity
{
    public Guid? ContactId { get; set; }

    [ForeignKey(nameof(ContactId))]
    public virtual Contact? Contact { get; set; }

    [Required]
    [MaxLength(200)]
    [Display(Name = "Title")]
    public string Title { get; set; } = string.Empty;

    [Display(Name = "Description")]
    public string? Description { get; set; }

    [Required]
    [Display(Name = "Due Date")]
    public DateOnly DueDate { get; set; }

    [Display(Name = "Completed")]
    public bool IsCompleted { get; set; }

    [Display(Name = "Completed Date")]
    public DateTime? CompletedDate { get; set; }
}