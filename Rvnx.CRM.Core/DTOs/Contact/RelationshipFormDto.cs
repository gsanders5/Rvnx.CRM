using System.ComponentModel.DataAnnotations;

namespace Rvnx.CRM.Core.DTOs.Contact;

public class RelationshipFormDto
{
    public Guid? Id { get; set; }
    public Guid ContactId { get; set; }
    public Guid RelatedContactId { get; set; }

    [Required]
    public Guid RelationshipTypeId { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    [DataType(DataType.Date)]
    public DateOnly? StartDate { get; set; }

    [DataType(DataType.Date)]
    public DateOnly? EndDate { get; set; }

    public List<string> SuggestedRelationships { get; set; } = [];
}
