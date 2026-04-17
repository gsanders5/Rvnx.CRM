using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rvnx.CRM.API.Helpers;
using Rvnx.CRM.Core.DTOs.Base;
using Rvnx.CRM.Core.Interfaces;
using System.Text.Json;

namespace Rvnx.CRM.API.Controllers;

/// <summary>
/// Manages notes attached to contacts. Notes support Markdown content.
/// Requires entityId (contact GUID) and entityType ("Person") when creating.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotesController(INoteService noteService) : ControllerBase
{
    private readonly INoteService _noteService = noteService;

    /// <summary>
    /// List all notes for a specific contact.
    /// </summary>
    /// <param name="contactId">The contact GUID.</param>
    [HttpGet("contact/{contactId}")]
    public async Task<IActionResult> ListByContact(Guid contactId)
    {
        List<Core.DTOs.Base.NoteDto> notes = await _noteService.GetByContactAsync(contactId);
        return Ok(notes);
    }

    /// <summary>
    /// Create a new note.
    /// </summary>
    /// <remarks>
    /// Required fields: title, value (the note body — supports Markdown), entityId, entityType ("Person").
    ///
    /// Example:
    ///
    ///     {
    ///       "entityId": "&lt;contact-id&gt;",
    ///       "entityType": "Person",
    ///       "title": "First meeting",
    ///       "value": "Met at the conference. Very knowledgeable about distributed systems."
    ///     }
    /// </remarks>
    /// <param name="model">The note data.</param>
    /// <returns>The new note's ID.</returns>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] NoteFormDto model)
    {
        var vm = new NoteFormViewModel
        {
            Title = model.Title,
            Value = model.Value,
            EntityId = model.EntityId,
            EntityType = model.EntityType
        };
        Core.Models.OperationResult result = await _noteService.CreateAsync(vm);
        return result.Success ? Ok(new { Id = result.RedirectId }) : BadRequest(new { Error = result.ErrorMessage });
    }

    /// <summary>
    /// Full update of a note. All fields are replaced.
    /// Use PATCH for partial updates.
    /// </summary>
    /// <param name="id">The note GUID.</param>
    /// <param name="model">The complete note data.</param>
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] NoteFormViewModel model)
    {
        model.Id = id;
        Core.Models.OperationResult result = await _noteService.UpdateAsync(id, model);
        return !result.Success ? BadRequest(new { Error = result.ErrorMessage }) : NoContent();
    }

    /// <summary>
    /// Partial update of a note using JSON Merge Patch (RFC 7396).
    /// Only include the fields you want to change.
    /// </summary>
    /// <param name="id">The note GUID.</param>
    /// <param name="patch">A JSON object containing only the fields to update.</param>
    [HttpPatch("{id}")]
    public async Task<IActionResult> Patch(Guid id, [FromBody] JsonElement patch)
    {
        NoteFormViewModel? existing = await _noteService.GetFormAsync(id);
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

        Core.Models.OperationResult result = await _noteService.UpdateAsync(id, existing);
        return !result.Success ? BadRequest(new { Error = result.ErrorMessage }) : NoContent();
    }

    /// <summary>
    /// Delete a note.
    /// </summary>
    /// <param name="id">The note GUID.</param>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        Core.Models.OperationResult result = await _noteService.DeleteAsync(id);
        return !result.Success ? BadRequest(new { Error = result.ErrorMessage }) : NoContent();
    }
}