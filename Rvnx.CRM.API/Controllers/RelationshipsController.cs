using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rvnx.CRM.Core.DTOs.Contact;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Core.Services;

namespace Rvnx.CRM.API.Controllers;

/// <summary>
/// Manages relationships between contacts (e.g., parent/child, spouse, sibling, friend).
/// Relationships are stored as a single record — the list endpoint returns both directions.
/// Use GET /api/relationships/types to discover available relationship type IDs.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RelationshipsController(IRelationshipService relationshipService, IContactReadService contactReadService) : ControllerBase
{
    private readonly IRelationshipService _relationshipService = relationshipService;
    private readonly IContactReadService _contactReadService = contactReadService;

    /// <summary>
    /// List all relationships for a contact, in both directions (where the contact is
    /// the source entity and where it is the related entity).
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
    /// Use the returned id in the selectedRelationshipType query parameter when creating
    /// or updating relationships. Format: "{id}_Fwd" for forward direction (e.g., "Parent")
    /// or "{id}_Rev" for reverse direction (e.g., "Child").
    /// </summary>
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
    /// The selectedRelationshipType query parameter must be in the format "{typeId}_Fwd" or "{typeId}_Rev".
    /// Use GET /api/relationships/types to discover available type IDs.
    /// For example, to create a "Parent" relationship: ?selectedRelationshipType={parentTypeId}_Fwd.
    /// For the reverse ("Child"): ?selectedRelationshipType={parentTypeId}_Rev.
    /// For symmetric types like "Sibling" or "Friend", _Fwd and _Rev are equivalent.
    /// </remarks>
    /// <param name="model">The relationship data. Required fields: entityId, relatedEntityId, entityType ("Person").</param>
    /// <param name="selectedRelationshipType">The relationship type and direction, e.g., "{typeId}_Fwd".</param>
    /// <returns>The ID of the related entity for navigation.</returns>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Relationship model, [FromQuery] string selectedRelationshipType)
    {
        RelationshipOperationResult result = await _relationshipService.CreateRelationshipAsync(model, selectedRelationshipType);
        if (!result.Success)
        {
            return BadRequest(new { Error = result.ErrorMessage });
        }
        return Ok(new { Id = result.RedirectId });
    }

    /// <summary>
    /// Full update of a relationship.
    /// </summary>
    /// <param name="id">The relationship GUID.</param>
    /// <param name="model">The updated relationship data.</param>
    /// <param name="selectedRelationshipType">The relationship type and direction, e.g., "{typeId}_Fwd".</param>
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] Relationship model, [FromQuery] string selectedRelationshipType)
    {
        RelationshipOperationResult result = await _relationshipService.UpdateRelationshipAsync(id, model, selectedRelationshipType);
        return !result.Success ? BadRequest(new { Error = result.ErrorMessage }) : NoContent();
    }

    /// <summary>
    /// Delete a relationship.
    /// </summary>
    /// <param name="id">The relationship GUID.</param>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        Core.Models.OperationResult result = await _relationshipService.DeleteRelationshipAsync(id);
        return !result.Success ? BadRequest(new { Error = result.ErrorMessage }) : NoContent();
    }
}
