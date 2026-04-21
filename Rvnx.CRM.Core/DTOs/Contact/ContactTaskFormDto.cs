using System.ComponentModel.DataAnnotations;

namespace Rvnx.CRM.Core.DTOs.Contact;

public class ContactTaskFormDto
{
    public Guid? Id { get; set; }

    public Guid EntityId { get; set; }

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
}
