using Rvnx.CRM.Core.DTOs.Common;
using Rvnx.CRM.Core.DTOs.Contact;
using Rvnx.CRM.Core.Models;
using Rvnx.CRM.Core.Models.Contact;

namespace Rvnx.CRM.Core.Interfaces;

public interface IRelationshipService
{
    /// <summary>
    /// Creates a new relationship between two contacts.
    /// Handles direction ("_Fwd" or "_Rev") by parsing the selected type string.
    /// </summary>
    Task<RelationshipOperationResult> CreateRelationshipAsync(Relationship relationship, string selectedRelationshipType, List<string>? suggestedContactIds = null);

    /// <summary>
    /// Updates an existing relationship.
    /// Handles direction ("_Fwd" or "_Rev") by parsing the selected type string.
    /// </summary>
    Task<RelationshipOperationResult> UpdateRelationshipAsync(Guid id, Relationship updatedRelationship, string selectedRelationshipType);

    /// <summary>
    /// Retrieves options for selecting a related contact, excluding the source contact itself.
    /// </summary>
    Task<List<SelectOptionDto>> GetRelatedContactOptionsAsync(Guid contactId, Guid? selectedId = null);

    /// <summary>
    /// Retrieves available relationship types formatted for selection.
    /// Includes both forward and reverse options for non-symmetric relationships.
    /// </summary>
    List<SelectOptionDto> GetRelationshipTypeOptions(string? selectedValue = null);

    /// <summary>
    /// Retrieves the list of all relationship type definitions.
    /// </summary>
    List<RelationshipTypeDefinition> GetRelationshipTypes();

    /// <summary>
    /// Creates a new "Partial Contact" and establishes a relationship to it.
    /// </summary>
    Task<RelationshipOperationResult> CreatePartialContactRelationshipAsync(Guid parentContactId, string selectedRelationshipType, CreatePartialContactRelationshipDto dto);

    /// <summary>
    /// Promotes a partial contact to a full contact by setting its IsPartial flag to false.
    /// </summary>
    Task<RelationshipOperationResult> PromotePartialContactAsync(Guid contactId);

    Task<Relationship?> GetRelationshipForEditAsync(Guid id);
    Task<OperationResult> DeleteRelationshipAsync(Guid id);
}
