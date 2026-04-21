using Rvnx.CRM.Core.DTOs.Contact;

namespace Rvnx.CRM.Core.Interfaces;

public interface IRelationshipSuggestionService
{
    /// <summary>
    /// Returns suggested additional relationships based on transitive or family-adult-child
    /// relationship topology for the given entity and relationship type.
    /// </summary>
    Task<List<SuggestedRelationshipDto>> GetSuggestedRelationshipsAsync(Guid entityId, Guid? relatedEntityId, Guid relationshipTypeId, bool isReverse, string? partialContactName);

    /// <summary>
    /// Returns true if a relationship of the given type already exists between the two entities
    /// in either direction. Pass <paramref name="excludeId"/> when editing to ignore the
    /// relationship being updated.
    /// </summary>
    Task<bool> RelationshipDuplicateExistsAsync(Guid entityId, Guid relatedEntityId, Guid typeId, Guid? excludeId = null);
}
