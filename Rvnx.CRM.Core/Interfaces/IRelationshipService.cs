using Rvnx.CRM.Core.DTOs.Common;
using Rvnx.CRM.Core.DTOs.Contact;
using Rvnx.CRM.Core.Enumerations;
using Rvnx.CRM.Core.Models;
using Rvnx.CRM.Core.Models.Contact;

namespace Rvnx.CRM.Core.Interfaces;

public interface IRelationshipService
{
    /// <summary>
    /// Creates a new relationship between two entities.
    /// Handles relationship direction ("_Fwd" or "_Rev") by parsing the selected type string.
    /// </summary>
    /// <param name="relationship">The relationship entity to create.</param>
    /// <param name="selectedRelationshipType">The selected relationship type (e.g., "{TypeId}_Fwd").</param>
    /// <returns>A <see cref="RelationshipOperationResult"/> indicating success or failure, with the ID of the redirect entity.</returns>
    Task<RelationshipOperationResult> CreateRelationshipAsync(Relationship relationship, string selectedRelationshipType, List<string>? suggestedEntityIds = null);

    /// <summary>
    /// Updates an existing relationship.
    /// Handles relationship direction ("_Fwd" or "_Rev") by parsing the selected type string.
    /// </summary>
    /// <param name="id">The ID of the relationship to update.</param>
    /// <param name="updatedRelationship">The updated relationship data.</param>
    /// <param name="selectedRelationshipType">The selected relationship type (e.g., "{TypeId}_Fwd").</param>
    /// <returns>A <see cref="RelationshipOperationResult"/> indicating success or failure, with the ID of the redirect entity.</returns>
    Task<RelationshipOperationResult> UpdateRelationshipAsync(Guid id, Relationship updatedRelationship, string selectedRelationshipType);

    /// <summary>
    /// Retrieves options for selecting a related entity, excluding the source entity itself.
    /// </summary>
    /// <param name="entityId">The ID of the entity that is the source of the relationship.</param>
    /// <param name="entityType">The type of the entity (e.g., Person, Company).</param>
    /// <param name="selectedId">The ID of the currently selected related entity, if any.</param>
    /// <returns>A list of <see cref="SelectOptionDto"/> representing available entities.</returns>
    Task<List<SelectOptionDto>> GetRelatedEntityOptionsAsync(Guid entityId, EntityType entityType, Guid? selectedId = null);

    /// <summary>
    /// Retrieves available relationship types for a given entity type, formatted for selection.
    /// Includes both forward and reverse options for non-symmetric relationships.
    /// </summary>
    /// <param name="entityType">The type of the source entity.</param>
    /// <param name="selectedValue">The currently selected value (e.g., "{TypeId}_Fwd"), if any.</param>
    /// <returns>A list of <see cref="SelectOptionDto"/> representing relationship types.</returns>
    List<SelectOptionDto> GetRelationshipTypeOptions(EntityType entityType, string? selectedValue = null);

    /// <summary>
    /// Retrieves the list of relationship type definitions for a given entity type.
    /// Used for building custom UI (e.g., two-step selection).
    /// </summary>
    /// <param name="entityType">The type of the source entity.</param>
    /// <returns>A list of <see cref="RelationshipTypeDefinition"/>.</returns>
    List<RelationshipTypeDefinition> GetRelationshipTypes(EntityType entityType);

    /// <summary>
    /// Creates a new "Partial Contact" and establishes a relationship to it.
    /// A partial contact is a contact that exists only as a name and potentially a birthday, without a full profile.
    /// </summary>
    /// <param name="parentEntityId">The ID of the entity initiating the relationship.</param>
    /// <param name="selectedRelationshipType">The selected relationship type string.</param>
    /// <param name="dto">Data for creating the partial contact.</param>
    /// <returns>A <see cref="RelationshipOperationResult"/> indicating success or failure.</returns>
    Task<RelationshipOperationResult> CreatePartialContactRelationshipAsync(Guid parentEntityId, string selectedRelationshipType, CreatePartialContactRelationshipDto dto);

    /// <summary>
    /// Promotes a partial contact to a full contact by setting its IsPartial flag to false.
    /// </summary>
    /// <param name="contactId">The ID of the contact to promote.</param>
    /// <returns>A <see cref="RelationshipOperationResult"/> indicating success or failure.</returns>
    Task<RelationshipOperationResult> PromotePartialContactAsync(Guid contactId);

    Task<Relationship?> GetRelationshipForEditAsync(Guid id);
    Task<Relationship?> GetRelationshipForDeleteAsync(Guid id);
    Task<OperationResult> DeleteRelationshipAsync(Guid id);
}