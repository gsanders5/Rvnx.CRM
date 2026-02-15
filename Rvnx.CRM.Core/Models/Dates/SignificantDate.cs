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
}
