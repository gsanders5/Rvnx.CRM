using Rvnx.CRM.Core.Models.Base;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rvnx.CRM.Core.Models.Business;

[Table("Employer")]
public class Employer : Company
{
    [Display(Name = "Job Title")]
    public string? JobTitle { get; set; } = string.Empty;

    [Display(Name = "Start Date")]
    public DateTime? StartDate { get; set; }

    [Display(Name = "End Date")]
    public DateTime? EndDate { get; set; }

    [Required]
    [Display(Name = "Employee ID")]
    public Guid EmployeeId { get; set; }

    [Required]
    [Display(Name = "Employee")]
    [ForeignKey(nameof(EmployeeId))]
    public virtual Person Employee { get; set; } = null!;
}