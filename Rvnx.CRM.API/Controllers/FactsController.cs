using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rvnx.CRM.API.Helpers;
using Rvnx.CRM.Core.DTOs.Contact;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models;
using System.Text.Json;

namespace Rvnx.CRM.API.Controllers;

/// <summary>
/// Manages facts (quick key-value info) for contacts. Each fact has a category and value.
/// Requires entityId (contact GUID) and entityType ("Person") when creating.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FactsController(IFactService factService) : ControllerBase
{
    private readonly IFactService _factService = factService;

    /// <summary>
    /// List all facts for a specific contact.
    /// </summary>
    /// <param name="contactId">The contact GUID.</param>
    [HttpGet("contact/{contactId}")]
    public async Task<IActionResult> ListByContact(Guid contactId)
    {
        List<FactDto> facts = await _factService.GetByContactAsync(contactId);
        return Ok(facts);
    }

    /// <summary>
    /// Create a new fact. Required fields: category, value, entityId, entityType ("Person").
    /// </summary>
    /// <remarks>
    /// Example: { "category": "Favorite Color", "value": "Blue", "entityId": "...", "entityType": "Person" }
    /// </remarks>
    /// <param name="model">The fact data.</param>
    /// <returns>The new fact's ID.</returns>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] FactFormDto model)
    {
        OperationResult result = await _factService.CreateAsync(model);
        return result.ToCreatedResult();
    }

    /// <summary>
    /// Full update of a fact. All fields are replaced.
    /// Use PATCH for partial updates.
    /// </summary>
    /// <param name="id">The fact GUID.</param>
    /// <param name="model">The complete fact data.</param>
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] FactFormDto model)
    {
        model.Id = id;
        OperationResult result = await _factService.UpdateAsync(id, model);
        return result.ToNoContentResult();
    }

    /// <summary>
    /// Partial update of a fact using JSON Merge Patch (RFC 7396).
    /// Only include the fields you want to change.
    /// </summary>
    /// <param name="id">The fact GUID.</param>
    /// <param name="patch">A JSON object containing only the fields to update.</param>
    [HttpPatch("{id}")]
    public async Task<IActionResult> Patch(Guid id, [FromBody] JsonElement patch)
    {
        FactFormDto? existing = await _factService.GetFormAsync(id);
        if (existing == null)
        {
            return NotFound();
        }

        IActionResult? validationFailure = JsonMergePatchHelper.ApplyAndValidate(existing, patch);
        if (validationFailure != null)
        {
            return validationFailure;
        }

        OperationResult result = await _factService.UpdateAsync(id, existing);
        return result.ToNoContentResult();
    }

    /// <summary>
    /// Delete a fact.
    /// </summary>
    /// <param name="id">The fact GUID.</param>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        OperationResult result = await _factService.DeleteAsync(id);
        return result.ToNoContentResult();
    }
}
