using System.ComponentModel.DataAnnotations;

namespace Rvnx.CRM.Core.DTOs.Contact;

public class PetFormDto
{
    public Guid? Id { get; set; }

    [Required]
    public Guid ContactId { get; set; }

    public List<Guid> ContactIds { get; set; } = [];

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? Species { get; set; }

    [MaxLength(100)]
    public string? Breed { get; set; }

    public DateOnly? Birthday { get; set; }

    public string? Notes { get; set; }
}
