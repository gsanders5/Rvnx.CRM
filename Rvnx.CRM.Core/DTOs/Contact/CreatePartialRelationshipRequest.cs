using Rvnx.CRM.Core.Enumerations;
using System.ComponentModel.DataAnnotations;

namespace Rvnx.CRM.Core.DTOs.Contact;

/// <summary>
/// Request body for creating a relationship together with a new partial contact.
/// Use GET /api/relationships/types to discover available RelationshipTypeId values.
/// </summary>
public class CreatePartialRelationshipRequest
{
    /// <summary>The relationship type GUID.</summary>
    [Required]
    public Guid RelationshipTypeId { get; set; }

    /// <summary>Forward applies the type's Name; Reverse applies its OppositeName.</summary>
    public CoreEnumerations.RelationshipDirection Direction { get; set; } = CoreEnumerations.RelationshipDirection.Forward;

    /// <summary>First name of the partial contact being created.</summary>
    [Required]
    [MaxLength(100)]
    public string PartialContactFirstName { get; set; } = string.Empty;

    /// <summary>Optional last name of the partial contact.</summary>
    [MaxLength(100)]
    public string? PartialContactLastName { get; set; }

    /// <summary>Optional birthday of the partial contact (creates a SignificantDate).</summary>
    public DateTime? Birthday { get; set; }

    /// <summary>Optional description of the relationship.</summary>
    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// Optional list of suggestion payloads (from GET /api/relationships/suggestions)
    /// to also create as transitive relationships.
    /// </summary>
    public List<string>? SuggestedRelationships { get; set; }
}
