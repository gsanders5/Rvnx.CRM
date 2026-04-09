using System.ComponentModel.DataAnnotations;

namespace Rvnx.CRM.Core.DTOs.Contact;

public class ActivityFormDto
{
    public Guid? Id { get; set; }

    [Required]
    public Guid EntityId { get; set; }

    public List<Guid> ContactIds { get; set; } = [];

    [Required]
    [MaxLength(200)]
    [Display(Name = "Title")]
    public string Title { get; set; } = string.Empty;

    [Display(Name = "Description")]
    public string? Description { get; set; }

    [Required]
    [Display(Name = "Date")]
    public DateTime ActivityDate { get; set; } = DateTime.Today;

    [MaxLength(100)]
    [Display(Name = "Type")]
    public string? ActivityType { get; set; }

    [MaxLength(200)]
    [Display(Name = "Location")]
    public string? Location { get; set; }
}