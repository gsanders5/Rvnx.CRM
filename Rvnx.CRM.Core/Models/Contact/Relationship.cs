using Rvnx.CRM.Core.Models.Base;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rvnx.CRM.Core.Models.Contact;

[Table("Relationship")]
public class Relationship : CRMBaseEntity
{
    [Required]
    [Display(Name = "Person")]
    public Guid PersonId { get; set; }

    [ForeignKey(nameof(PersonId))]
    public virtual Person? Person { get; set; }

    [Required]
    [Display(Name = "Related Person")]
    public Guid RelatedPersonId { get; set; }

    [ForeignKey(nameof(RelatedPersonId))]
    public virtual Person? RelatedPerson { get; set; }

    [Required]
    [Display(Name = "Relationship Type")]
    public Guid RelationshipTypeId { get; set; }

    [ForeignKey(nameof(RelationshipTypeId))]
    public virtual RelationshipType? RelationshipType { get; set; }

    [Display(Name = "Description")]
    public string? Description { get; set; }

    [Display(Name = "Start Date")]
    public DateTime? StartDate { get; set; }

    [Display(Name = "End Date")]
    public DateTime? EndDate { get; set; }
}
