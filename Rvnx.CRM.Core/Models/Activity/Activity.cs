using Rvnx.CRM.Core.Models.Base;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rvnx.CRM.Core.Models.Activity;

[Table("Activity")]
public class Activity : BaseEntity
{
    [Required]
    [MaxLength(200)]
    [Display(Name = "Title")]
    public string Title { get; set; } = string.Empty;

    [Display(Name = "Description")]
    public string? Description { get; set; }

    [Required]
    [Display(Name = "Date")]
    public DateTime ActivityDate { get; set; }

    [MaxLength(100)]
    [Display(Name = "Type")]
    public string? ActivityType { get; set; }

    [MaxLength(200)]
    [Display(Name = "Location")]
    public string? Location { get; set; }

    public virtual ICollection<ActivityContact> ActivityContacts { get; set; } = [];
}