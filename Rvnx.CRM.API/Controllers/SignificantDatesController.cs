using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rvnx.CRM.API.Helpers;
using Rvnx.CRM.Core.DTOs.Dates;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models;
using System.Text.Json;

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
    /// Create a new significant date.
    /// </summary>
    /// <remarks>
    /// Required fields: title, eventDate, entityId, entityType ("Person").
    ///
    /// RecurrenceType values: None, Annual, Monthly, Custom.
    /// Use Custom with customIntervalDays to set an arbitrary repeat interval.
    /// ReminderOffsetDays is a list of integers (days before the event to send a reminder), e.g. [7, 1].
    ///
    /// Example — add an annual birthday:
    ///
    ///     {
    ///       "entityId": "&lt;contact-id&gt;",
    ///       "entityType": "Person",
    ///       "title": "Birthday",
    ///       "eventDate": "1990-06-15",
    ///       "recurrenceType": "Annual"
    ///     }
    /// </remarks>
    /// <param name="request">The significant date data.</param>
    /// <returns>The new significant date's ID.</returns>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateSignificantDateRequest request)
    {
        SignificantDateDto model = new()
        {
            EntityId = request.EntityId,
            Title = request.Title,
            Description = request.Description,
            EventDate = request.EventDate,
            RecurrenceType = request.RecurrenceType,
            CustomIntervalDays = request.CustomIntervalDays,
            IsActive = true
        };

        OperationResult result = await _significantDateService.CreateAsync(model);
        return result.ToCreatedResult();
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
        OperationResult result = await _significantDateService.UpdateAsync(id, model);
        return result.ToNoContentResult();
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

        IActionResult? validationFailure = JsonMergePatchHelper.ApplyAndValidate(existing, patch);
        if (validationFailure != null)
        {
            return validationFailure;
        }

        OperationResult result = await _significantDateService.UpdateAsync(id, existing);
        return result.ToNoContentResult();
    }

    /// <summary>
    /// Delete a significant date.
    /// </summary>
    /// <param name="id">The significant date GUID.</param>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        OperationResult result = await _significantDateService.DeleteAsync(id);
        return result.ToNoContentResult();
    }
}
