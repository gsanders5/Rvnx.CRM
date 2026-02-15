using Rvnx.CRM.Core.Models.Base;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rvnx.CRM.Core.Models.Contact;

[Table("Relationship")]
public class Relationship : CRMGenericEntity
{
    [Required]
    [Display(Name = "Related Entity ID")]
    public Guid RelatedEntityId { get; set; }

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

    [NotMapped]
    public virtual Person? Person { get; set; }

    [NotMapped]
    public virtual Person? RelatedPerson { get; set; }
}
