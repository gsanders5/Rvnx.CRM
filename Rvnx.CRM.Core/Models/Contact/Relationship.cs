using Rvnx.CRM.Core.Models.Base;
using Rvnx.CRM.Core.Services;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rvnx.CRM.Core.Models.Contact;

[Table("Relationship")]
public class Relationship : PolymorphicEntity
{
    [Display(Name = "Related Entity ID")]
    public Guid? RelatedEntityId { get; set; }

    [Display(Name = "Relationship Type")]
    public Guid RelationshipTypeId { get; set; }

    [Display(Name = "Description")]
    public string? Description { get; set; }

    [Display(Name = "Start Date")]
    public DateTime? StartDate { get; set; }

    [Display(Name = "End Date")]
    public DateTime? EndDate { get; set; }

    [MaxLength(100)]
    public string? PartialContactFirstName { get; set; }

    [MaxLength(100)]
    public string? PartialContactLastName { get; set; }

    public DateTime? PartialContactDateOfBirth { get; set; }

    public bool IsPartialContact => RelatedEntityId is null;

    // Indicates that the relationship direction is reversed relative to the defined Type.
    // Used primarily for Partial Contacts where entities cannot be swapped.
    public bool IsTypeReverse { get; set; }

    [NotMapped]
    public virtual Person? Person { get; set; }

    [NotMapped]
    public virtual Person? RelatedPerson { get; set; }

    // Helper properties for UI convenience, looking up from static service
    [NotMapped]
    public string RelationshipTypeName =>
        IsTypeReverse
            ? (RelationshipTypeService.GetById(RelationshipTypeId)?.OppositeName ?? "Unknown")
            : (RelationshipTypeService.GetById(RelationshipTypeId)?.Name ?? "Unknown");

    [NotMapped]
    public string RelationshipTypeOppositeName =>
        IsTypeReverse
            ? (RelationshipTypeService.GetById(RelationshipTypeId)?.Name ?? "Unknown")
            : (RelationshipTypeService.GetById(RelationshipTypeId)?.OppositeName ?? "Unknown");

    [NotMapped]
    public string RelationshipTypeCategory =>
        RelationshipTypeService.GetById(RelationshipTypeId)?.Category ?? "Other";
}
