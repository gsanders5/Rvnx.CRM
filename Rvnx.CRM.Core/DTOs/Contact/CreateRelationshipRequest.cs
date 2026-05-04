using Rvnx.CRM.Core.Enumerations;
using System.ComponentModel.DataAnnotations;

namespace Rvnx.CRM.Core.DTOs.Contact;

/// <summary>
/// Request body for creating or updating a relationship between two contacts.
/// Use GET /api/relationships/types to discover available RelationshipTypeId values.
/// </summary>
public class CreateRelationshipRequest
{
    /// <summary>The contact who is the source of the relationship.</summary>
    [Required]
    public Guid ContactId { get; set; }

    /// <summary>The contact who is the target of the relationship.</summary>
    [Required]
    public Guid RelatedContactId { get; set; }

    /// <summary>
    /// The relationship type GUID. Use GET /api/relationships/types to list all types with their IDs,
    /// names, opposite names, and whether they are symmetric.
    /// </summary>
    [Required]
    public Guid RelationshipTypeId { get; set; }

    /// <summary>
    /// Which direction to apply. Forward uses the type's Name (e.g. "Parent"),
    /// Reverse uses its OppositeName (e.g. "Child"). For symmetric types (Friend, Sibling,
    /// Colleague) the choice makes no difference.
    /// </summary>
    public CoreEnumerations.RelationshipDirection Direction { get; set; } = CoreEnumerations.RelationshipDirection.Forward;

    /// <summary>Optional notes about the relationship.</summary>
    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>Optional date when the relationship began.</summary>
    public DateOnly? StartDate { get; set; }

    /// <summary>Optional date when the relationship ended.</summary>
    public DateOnly? EndDate { get; set; }
}
