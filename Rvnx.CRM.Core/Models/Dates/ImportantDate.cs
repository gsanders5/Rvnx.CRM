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

    [Display(Name = "Entity ID")]
    public Guid? EntityId { get; set; }

    [MaxLength(100)]
    [Display(Name = "Entity Type")]
    public string? EntityType { get; set; }
}
