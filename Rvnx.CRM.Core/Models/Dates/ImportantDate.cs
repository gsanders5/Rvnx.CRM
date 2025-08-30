using Rvnx.CRM.Core.Models.Base;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rvnx.CRM.Core.Models.Dates;

[Table("ImportantDate")]
public class ImportantDate : CRMBaseEntity
{
    [MaxLength(200)]
    [Display(Name = "Title")]
    public string? Title { get; set; }

    [Display(Name = "Description")]
    public string? Description { get; set; }

    [Required]
    [Display(Name = "Date")]
    public DateTime Date { get; set; }

    [Display(Name = "Person ID")]
    public Guid? PersonId { get; set; }

    [Display(Name = "Person")]
    [ForeignKey(nameof(PersonId))]
    public virtual Person? Person { get; set; }

    [Display(Name = "Company ID")]
    public Guid? CompanyId { get; set; }

    [Display(Name = "Company")]
    [ForeignKey(nameof(CompanyId))]
    public virtual Company? Company { get; set; }
}
