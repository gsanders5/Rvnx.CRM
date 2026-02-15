using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rvnx.CRM.Core.Models.Base;

[Table("Note")]
public class Note : CRMGenericEntity
{
    [Required]
    [MaxLength(200)]
    [Display(Name = "Title")]
    public string Title { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Note")]
    public string Value { get; set; } = string.Empty;
}
