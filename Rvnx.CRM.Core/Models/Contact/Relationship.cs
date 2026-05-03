using Rvnx.CRM.Core.Models.Base;
using Rvnx.CRM.Core.Services;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rvnx.CRM.Core.Models.Contact;

[Table("Relationship")]
public class Relationship : BaseEntity
{
    [Display(Name = "Contact ID")]
    public Guid ContactId { get; set; }

    [Display(Name = "Related Contact ID")]
    public Guid RelatedContactId { get; set; }

    [Display(Name = "Relationship Type")]
    public Guid RelationshipTypeId { get; set; }

    [Display(Name = "Description")]
    public string? Description { get; set; }

    [Display(Name = "Start Date")]
    public DateOnly? StartDate { get; set; }

    [Display(Name = "End Date")]
    public DateOnly? EndDate { get; set; }

    /// <summary>
    /// Gets or sets the source person.
    /// This property is [NotMapped] and MUST be populated manually by a service.
    /// </summary>
    [NotMapped]
    public virtual Person? Person { get; set; }

    /// <summary>
    /// Gets or sets the related (target) person.
    /// This property is [NotMapped] and MUST be populated manually by a service.
    /// </summary>
    [NotMapped]
    public virtual Person? RelatedPerson { get; set; }

    [NotMapped]
    public string RelationshipTypeName => TypeDefinition?.Name ?? "Unknown";

    [NotMapped]
    public string RelationshipTypeOppositeName => TypeDefinition?.OppositeName ?? "Unknown";

    [NotMapped]
    public string RelationshipTypeCategory => TypeDefinition?.Category ?? "Other";

    private RelationshipTypeDefinition? TypeDefinition => RelationshipTypeService.GetById(RelationshipTypeId);
}
