using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rvnx.CRM.API.Helpers;
using Rvnx.CRM.Core.DTOs.Contact;
using Rvnx.CRM.Core.Interfaces;

namespace Rvnx.CRM.API.Controllers;

/// <summary>
/// Manages labels (tags) and their assignment to contacts.
/// Labels have a many-to-many relationship with contacts.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LabelsController(ILabelService labelService) : ControllerBase
{
    private readonly ILabelService _labelService = labelService;

    /// <summary>
    /// List all labels available to the current user.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> List()
    {
        List<LabelDto> labels = await _labelService.GetAllAsync();
        return Ok(labels);
    }

    /// <summary>
    /// Create a new label. Required field: name. Optional: color (hex code, e.g. "#FF5733").
    /// </summary>
    /// <param name="model">The label data.</param>
    /// <returns>The new label's ID.</returns>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] LabelFormDto model)
    {
        LabelOperationResult result = await _labelService.CreateAsync(model.Name, model.Color);
        if (!result.Success)
        {
            return BadRequest(new { result.Errors });
        }
        return Ok(new { Id = result.LabelId });
    }

    /// <summary>
    /// Full update of a label. All fields are replaced.
    /// Use PATCH for partial updates.
    /// </summary>
    /// <param name="id">The label GUID.</param>
    /// <param name="model">The complete label data.</param>
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] LabelFormDto model)
    {
        LabelOperationResult result = await _labelService.UpdateAsync(id, model.Name, model.Color);
        return !result.Success ? result.IsNotFound ? NotFound() : BadRequest(new { result.Errors }) : NoContent();
    }

    /// <summary>
    /// Partial update of a label using JSON Merge Patch (RFC 7396).
    /// Only include the fields you want to change.
    /// </summary>
    /// <param name="id">The label GUID.</param>
    /// <param name="patch">A JSON object containing only the fields to update.</param>
    [HttpPatch("{id}")]
    public async Task<IActionResult> Patch(Guid id, [FromBody] JsonElement patch)
    {
        LabelDto? label = await _labelService.GetByIdAsync(id);
        if (label == null)
        {
            return NotFound();
        }

        LabelFormDto existing = new() { Id = id, Name = label.Name, Color = label.Color };
        JsonMergePatchHelper.ApplyPatch(existing, patch);

        List<string> errors = JsonMergePatchHelper.Validate(existing);
        if (errors.Count > 0)
        {
            return BadRequest(new { Errors = errors });
        }

        LabelOperationResult result = await _labelService.UpdateAsync(id, existing.Name, existing.Color);
        return !result.Success ? result.IsNotFound ? NotFound() : BadRequest(new { result.Errors }) : NoContent();
    }

    /// <summary>
    /// Delete a label. Removes it from all contacts that have it assigned.
    /// </summary>
    /// <param name="id">The label GUID.</param>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _labelService.DeleteAsync(id);
        return NoContent();
    }

    /// <summary>
    /// Assign a label to a contact. If already assigned, does nothing.
    /// </summary>
    /// <param name="contactId">The contact GUID.</param>
    /// <param name="labelId">The label GUID.</param>
    [HttpPost("contact/{contactId}/{labelId}")]
    public async Task<IActionResult> Associate(Guid contactId, Guid labelId)
    {
        await _labelService.AssignLabelAsync(contactId, labelId);
        return NoContent();
    }

    /// <summary>
    /// Remove a label from a contact.
    /// </summary>
    /// <param name="contactId">The contact GUID.</param>
    /// <param name="labelId">The label GUID.</param>
    [HttpDelete("contact/{contactId}/{labelId}")]
    public async Task<IActionResult> Disassociate(Guid contactId, Guid labelId)
    {
        await _labelService.RemoveLabelAsync(contactId, labelId);
        return NoContent();
    }
}
