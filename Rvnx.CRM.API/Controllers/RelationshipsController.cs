using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rvnx.CRM.API.Helpers;
using Rvnx.CRM.Core.DTOs.Contact;
using Rvnx.CRM.Core.Enumerations;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Core.Services;

namespace Rvnx.CRM.API.Controllers;

/// <summary>
/// Manages relationships between contacts (e.g. Parent/Child, Spouse, Friend, Colleague).
/// Relationships are directional but the list endpoint returns both directions.
/// Call GET /api/relationships/types first to discover relationship type IDs.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RelationshipsController(IRelationshipService relationshipService, IContactReadService contactReadService) : ControllerBase
{
    private readonly IRelationshipService _relationshipService = relationshipService;
    private readonly IContactReadService _contactReadService = contactReadService;

    /// <summary>
    /// List all relationships for a contact, in both directions.
    /// </summary>
    /// <param name="contactId">The contact GUID.</param>
    [HttpGet("contact/{contactId}")]
    public async Task<IActionResult> ListByContact(Guid contactId)
    {
        ContactDetailDto? contactDetails = await _contactReadService.GetContactDetailsAsync(contactId);
        if (contactDetails == null)
        {
            return NotFound();
        }

        List<RelationshipDto> allRelationships = [.. contactDetails.Relationships, .. contactDetails.RelatedTo];
        return Ok(allRelationships);
    }

    /// <summary>
    /// List all available relationship types with their IDs, names, and categories.
    /// Use the returned id as RelationshipTypeId when creating or updating relationships.
    /// </summary>
    /// <remarks>
    /// Each type has a Name and OppositeName. For asymmetric types (e.g. Parent/Child) use
    /// Direction=Forward to apply the Name to EntityId, or Direction=Reverse to apply OppositeName.
    /// For symmetric types (Friend, Sibling, Colleague) Direction is ignored.
    /// </remarks>
    [HttpGet("types")]
    public IActionResult ListTypes()
    {
        var types = RelationshipTypeService.GetAll().Select(t => new
        {
            t.Id,
            t.Name,
            t.OppositeName,
            t.Category,
            t.IsSymmetric
        });
        return Ok(types);
    }

    /// <summary>
    /// Create a new relationship between two contacts.
    /// </summary>
    /// <remarks>
    /// Workflow:
    /// 1. Call GET /api/relationships/types to find the RelationshipTypeId you want.
    /// 2. Set Direction=Forward to apply the type's Name to EntityId (e.g. EntityId is the "Parent").
    ///    Set Direction=Reverse to apply OppositeName (e.g. EntityId is the "Child").
    ///    For symmetric types (Friend, Colleague, Sibling) direction does not matter.
    ///
    /// Example — create a Parent/Child relationship where Alice is the parent of Bob:
    ///
    ///     POST /api/relationships
    ///     {
    ///       "entityId": "&lt;alice-id&gt;",
    ///       "relatedEntityId": "&lt;bob-id&gt;",
    ///       "entityType": "Person",
    ///       "relationshipTypeId": "7c1f8d22-1b6a-4c28-9c1e-3f5a2b8e9d1a",
    ///       "direction": "Forward"
    ///     }
    /// </remarks>
    /// <param name="request">The relationship data.</param>
    /// <returns>The ID of the new relationship record.</returns>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateRelationshipRequest request)
    {
        RelationshipOperationResult result = await _relationshipService.CreateRelationshipAsync(
            MapToRelationship(request),
            BuildSelectedType(request.RelationshipTypeId, request.Direction));

        return result.Success
            ? Ok(new { Id = result.RedirectId })
            : BadRequest(new { Error = result.ErrorMessage });
    }

    /// <summary>
    /// Update an existing relationship.
    /// </summary>
    /// <param name="id">The relationship GUID.</param>
    /// <param name="request">The updated relationship data.</param>
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] CreateRelationshipRequest request)
    {
        RelationshipOperationResult result = await _relationshipService.UpdateRelationshipAsync(
            id,
            MapToRelationship(request),
            BuildSelectedType(request.RelationshipTypeId, request.Direction));

        return result.Success
            ? NoContent()
            : BadRequest(new { Error = result.ErrorMessage });
    }

    /// <summary>
    /// Delete a relationship.
    /// </summary>
    /// <param name="id">The relationship GUID.</param>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        OperationResult result = await _relationshipService.DeleteRelationshipAsync(id);
        return result.ToNoContentResult();
    }

    private static Relationship MapToRelationship(CreateRelationshipRequest r)
    {
        return new()
        {
            EntityId = r.EntityId,
            RelatedEntityId = r.RelatedEntityId,
            EntityType = r.EntityType,
            Description = r.Description,
            StartDate = r.StartDate,
            EndDate = r.EndDate
        };
    }

    // Converts the typed (TypeId, Direction) pair exposed by the API into the
    // internal "{TypeId}_Fwd" / "{TypeId}_Rev" string key the service consumes.
    private static string BuildSelectedType(Guid typeId, CoreEnumerations.RelationshipDirection direction)
    {
        string suffix = direction == CoreEnumerations.RelationshipDirection.Reverse ? "Rev" : "Fwd";
        return $"{typeId}_{suffix}";
    }
}
