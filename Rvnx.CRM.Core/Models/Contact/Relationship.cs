using Rvnx.CRM.Core.Models.Base;
using Rvnx.CRM.Core.Services;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rvnx.CRM.Core.Models.Contact;

[Table("Relationship")]
public class Relationship : PolymorphicEntity
{
    [Required]
    [Display(Name = "Related Entity ID")]
    public Guid RelatedEntityId { get; set; }

    [Required]
    [Display(Name = "Relationship Type")]
    public Guid RelationshipTypeId { get; set; }

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

    [NotMapped]
    public string RelationshipTypeName =>
        RelationshipTypeService.GetById(RelationshipTypeId)?.Name ?? "Unknown";

    [NotMapped]
    public string RelationshipTypeOppositeName =>
        RelationshipTypeService.GetById(RelationshipTypeId)?.OppositeName ?? "Unknown";

    [NotMapped]
    public string RelationshipTypeCategory =>
        RelationshipTypeService.GetById(RelationshipTypeId)?.Category ?? "Other";
}
