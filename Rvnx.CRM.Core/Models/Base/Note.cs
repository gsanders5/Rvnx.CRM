using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rvnx.CRM.Core.Models.Base;

[Table("Note")]
public class Note : CRMBaseEntity
{
    [Required]
    [MaxLength(200)]
    [Display(Name = "Title")]
    public string Title { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Note")]
    public string Value { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Person ID")]
    public Guid PersonId { get; set; }

    [Display(Name = "Person")]
    [ForeignKey(nameof(PersonId))]
    public virtual Person? Person { get; set; }
}