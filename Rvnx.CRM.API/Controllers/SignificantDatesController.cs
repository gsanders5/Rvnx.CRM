using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rvnx.CRM.API.Helpers;
using Rvnx.CRM.Core.DTOs.Dates;
using Rvnx.CRM.Core.Interfaces;

namespace Rvnx.CRM.API.Controllers;

/// <summary>
/// Manages significant dates (birthdays, anniversaries, custom dates) for contacts.
/// Dates can have recurrence patterns and reminder offsets.
/// Requires entityId (contact GUID) and entityType ("Person") when creating.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SignificantDatesController(ISignificantDateService significantDateService) : ControllerBase
{
    private readonly ISignificantDateService _significantDateService = significantDateService;

    /// <summary>
    /// List all significant dates for a specific contact.
    /// </summary>
    /// <param name="contactId">The contact GUID.</param>
    [HttpGet("contact/{contactId}")]
    public async Task<IActionResult> ListByContact(Guid contactId)
    {
        List<SignificantDateDto> dates = await _significantDateService.GetByContactAsync(contactId);
        return Ok(dates);
    }

    /// <summary>
    /// Create a new significant date. Required fields: title, eventDate, entityId, entityType ("Person").
    /// </summary>
    /// <param name="model">The significant date data.</param>
    /// <returns>The new significant date's ID.</returns>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] SignificantDateDto model)
    {
        Core.Models.OperationResult result = await _significantDateService.CreateAsync(model);
        if (!result.Success)
        {
            return BadRequest(new { Error = result.ErrorMessage });
        }
        return Ok(new { Id = result.RedirectId });
    }

    /// <summary>
    /// Full update of a significant date. All fields are replaced.
    /// Use PATCH for partial updates.
    /// </summary>
    /// <param name="id">The significant date GUID.</param>
    /// <param name="model">The complete significant date data.</param>
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] SignificantDateDto model)
    {
        model.Id = id;
        Core.Models.OperationResult result = await _significantDateService.UpdateAsync(id, model);
        return !result.Success ? BadRequest(new { Error = result.ErrorMessage }) : NoContent();
    }

    /// <summary>
    /// Partial update of a significant date using JSON Merge Patch (RFC 7396).
    /// Only include the fields you want to change.
    /// </summary>
    /// <param name="id">The significant date GUID.</param>
    /// <param name="patch">A JSON object containing only the fields to update.</param>
    [HttpPatch("{id}")]
    public async Task<IActionResult> Patch(Guid id, [FromBody] JsonElement patch)
    {
        SignificantDateDto? existing = await _significantDateService.GetDtoAsync(id);
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

        Core.Models.OperationResult result = await _significantDateService.UpdateAsync(id, existing);
        return !result.Success ? BadRequest(new { Error = result.ErrorMessage }) : NoContent();
    }

    /// <summary>
    /// Delete a significant date.
    /// </summary>
    /// <param name="id">The significant date GUID.</param>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        Core.Models.OperationResult result = await _significantDateService.DeleteAsync(id);
        return !result.Success ? BadRequest(new { Error = result.ErrorMessage }) : NoContent();
    }
}
