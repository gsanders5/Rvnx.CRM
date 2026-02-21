using Rvnx.CRM.Core.DTOs.Common;
using Rvnx.CRM.Core.Models.Contact;

namespace Rvnx.CRM.Core.Interfaces;

public interface IRelationshipService
{
    Task<RelationshipOperationResult> CreateRelationshipAsync(Relationship relationship, string selectedRelationshipType);
    Task<RelationshipOperationResult> UpdateRelationshipAsync(Guid id, Relationship updatedRelationship, string selectedRelationshipType);
    
    Task<List<SelectOptionDto>> GetRelatedEntityOptionsAsync(Guid entityId, string entityType, Guid? selectedId = null);
    List<SelectOptionDto> GetRelationshipTypeOptions(string entityType, string? selectedValue = null);
}
