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
public class RelationshipsController(
    IRelationshipService relationshipService,
    IRelationshipSuggestionService relationshipSuggestionService,
    IContactReadService contactReadService) : ControllerBase
{
    private readonly IRelationshipService _relationshipService = relationshipService;
    private readonly IRelationshipSuggestionService _relationshipSuggestionService = relationshipSuggestionService;
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
    /// Direction=Forward to apply the Name to ContactId, or Direction=Reverse to apply OppositeName.
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
    /// 2. Set Direction=Forward to apply the type's Name to ContactId (e.g. ContactId is the "Parent").
    ///    Set Direction=Reverse to apply OppositeName (e.g. ContactId is the "Child").
    ///    For symmetric types (Friend, Colleague, Sibling) direction does not matter.
    ///
    /// Example — create a Parent/Child relationship where Alice is the parent of Bob:
    ///
    ///     POST /api/relationships
    ///     {
    ///       "contactId": "&lt;alice-id&gt;",
    ///       "relatedContactId": "&lt;bob-id&gt;",
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

    /// <summary>
    /// Create a relationship together with a new partial contact (a placeholder
    /// person who exists only to anchor the relationship). Returns the parent
    /// contact's ID so callers can refresh the contact view.
    /// </summary>
    /// <remarks>
    /// Use this when the related person isn't a full contact yet — e.g. "John's wife"
    /// before her own profile is created. Promote the partial contact later via
    /// POST /api/relationships/{contactId}/promote.
    /// </remarks>
    /// <param name="contactId">The parent contact GUID (the existing contact).</param>
    /// <param name="request">Relationship type/direction and the partial contact's details.</param>
    [HttpPost("contact/{contactId}/partial")]
    public async Task<IActionResult> CreatePartial(Guid contactId, [FromBody] CreatePartialRelationshipRequest request)
    {
        CreatePartialContactRelationshipDto dto = new()
        {
            SelectedRelationshipType = BuildSelectedType(request.RelationshipTypeId, request.Direction),
            PartialContactFirstName = request.PartialContactFirstName,
            PartialContactLastName = request.PartialContactLastName,
            Birthday = request.Birthday,
            Description = request.Description,
            SuggestedRelationships = request.SuggestedRelationships ?? []
        };

        RelationshipOperationResult result = await _relationshipService.CreatePartialContactRelationshipAsync(
            contactId, dto.SelectedRelationshipType, dto);

        return result.Success
            ? Ok(new { Id = result.RedirectId })
            : BadRequest(new { Error = result.ErrorMessage });
    }

    /// <summary>
    /// Promote a partial contact to a full contact (clears the IsPartial flag).
    /// </summary>
    /// <param name="contactId">The partial contact's GUID.</param>
    [HttpPost("contact/{contactId}/promote")]
    public async Task<IActionResult> Promote(Guid contactId)
    {
        RelationshipOperationResult result = await _relationshipService.PromotePartialContactAsync(contactId);
        return result.Success
            ? NoContent()
            : BadRequest(new { Error = result.ErrorMessage });
    }

    /// <summary>
    /// Discover suggested transitive or family relationships that follow from the
    /// given seed (e.g. siblings of an existing sibling, the other parent's adult-child
    /// links). Returns a list of payload-tagged suggestions that the caller can echo
    /// back as <c>SuggestedRelationships</c> when creating the primary relationship.
    /// </summary>
    /// <param name="contactId">The source contact GUID.</param>
    /// <param name="relatedContactId">The target contact GUID, if known.</param>
    /// <param name="relationshipTypeId">The relationship type GUID.</param>
    /// <param name="direction">Forward or Reverse — same convention as POST /api/relationships.</param>
    /// <param name="partialContactName">Optional display name when the related contact is a not-yet-created partial.</param>
    [HttpGet("suggestions")]
    public async Task<IActionResult> GetSuggestions(
        [FromQuery] Guid contactId,
        [FromQuery] Guid? relatedContactId,
        [FromQuery] Guid relationshipTypeId,
        [FromQuery] CoreEnumerations.RelationshipDirection direction = CoreEnumerations.RelationshipDirection.Forward,
        [FromQuery] string? partialContactName = null)
    {
        bool isReverse = direction == CoreEnumerations.RelationshipDirection.Reverse;
        List<SuggestedRelationshipDto> suggestions = await _relationshipSuggestionService
            .GetSuggestedRelationshipsAsync(contactId, relatedContactId, relationshipTypeId, isReverse, partialContactName);
        return Ok(suggestions);
    }

    private static Relationship MapToRelationship(CreateRelationshipRequest r)
    {
        return new()
        {
            ContactId = r.ContactId,
            RelatedContactId = r.RelatedContactId,
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
