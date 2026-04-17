using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rvnx.CRM.API.Helpers;
using Rvnx.CRM.Core.DTOs.Contact;
using Rvnx.CRM.Core.Interfaces;
using System.Text.Json;

namespace Rvnx.CRM.API.Controllers;

/// <summary>
/// Manages activities (events, meetings, interactions). Activities have a many-to-many relationship
/// with contacts via the contactIds field.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ActivitiesController(IActivityService activityService) : ControllerBase
{
    private readonly IActivityService _activityService = activityService;

    /// <summary>
    /// List all activities associated with a specific contact.
    /// </summary>
    /// <param name="contactId">The contact GUID.</param>
    [HttpGet("contact/{contactId}")]
    public async Task<IActionResult> ListByContact(Guid contactId)
    {
        List<ActivityDto> activities = await _activityService.GetByContactAsync(contactId);
        return Ok(activities);
    }

    /// <summary>
    /// Create a new activity. Required fields: title, activityDate, entityId.
    /// The contactIds array links the activity to one or more contacts.
    /// </summary>
    /// <param name="model">The activity data.</param>
    /// <returns>The new activity's ID.</returns>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ActivityFormDto model)
    {
        Core.Models.OperationResult result = await _activityService.CreateAsync(model);
        if (!result.Success)
        {
            return BadRequest(new { Error = result.ErrorMessage });
        }
        return Ok(new { Id = result.RedirectId });
    }

    /// <summary>
    /// Full update of an activity. All fields are replaced.
    /// Use PATCH for partial updates.
    /// </summary>
    /// <param name="id">The activity GUID.</param>
    /// <param name="model">The complete activity data.</param>
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] ActivityFormDto model)
    {
        model.Id = id;
        Core.Models.OperationResult result = await _activityService.UpdateAsync(id, model);
        return !result.Success ? BadRequest(new { Error = result.ErrorMessage }) : NoContent();
    }

    /// <summary>
    /// Partial update of an activity using JSON Merge Patch (RFC 7396).
    /// Only include the fields you want to change.
    /// </summary>
    /// <param name="id">The activity GUID.</param>
    /// <param name="patch">A JSON object containing only the fields to update.</param>
    [HttpPatch("{id}")]
    public async Task<IActionResult> Patch(Guid id, [FromBody] JsonElement patch)
    {
        ActivityFormDto? existing = await _activityService.GetFormAsync(id);
        if (existing == null)
        {
            return NotFound();
        }

        JsonMergePatchHelper.ApplyPatch(existing, patch);

        List<string> errors = JsonMergePatchHelper.Validate(existing);
        if (errors.Count > 0)
        {
            return BadRequest(new { Errors = errors });
        }

        Core.Models.OperationResult result = await _activityService.UpdateAsync(id, existing);
        return !result.Success ? BadRequest(new { Error = result.ErrorMessage }) : NoContent();
    }

    /// <summary>
    /// Delete an activity.
    /// </summary>
    /// <param name="id">The activity GUID.</param>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        Core.Models.OperationResult result = await _activityService.DeleteAsync(id);
        return !result.Success ? BadRequest(new { Error = result.ErrorMessage }) : NoContent();
    }
}