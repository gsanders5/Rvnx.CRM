using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rvnx.CRM.API.Helpers;
using Rvnx.CRM.Core.DTOs.Contact;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models;
using System.Text.Json;

namespace Rvnx.CRM.API.Controllers;

/// <summary>
/// Manages contact methods (phone numbers, emails, social media, etc.) for contacts.
/// Requires entityId (contact GUID) and entityType ("Person") when creating.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ContactMethodsController(IContactMethodService contactMethodService) : ControllerBase
{
    private readonly IContactMethodService _contactMethodService = contactMethodService;

    /// <summary>
    /// List all contact methods for a specific contact.
    /// </summary>
    /// <param name="contactId">The contact GUID.</param>
    [HttpGet("contact/{contactId}")]
    public async Task<IActionResult> ListByContact(Guid contactId)
    {
        List<ContactMethodDto> methods = await _contactMethodService.GetByContactAsync(contactId);
        return Ok(methods);
    }

    /// <summary>
    /// Create a new contact method. Required fields: type (enum), value, entityId, entityType ("Person").
    /// </summary>
    /// <param name="model">The contact method data.</param>
    /// <returns>The new contact method's ID.</returns>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ContactMethodFormDto model)
    {
        OperationResult result = await _contactMethodService.CreateAsync(model);
        if (!result.Success)
        {
            return BadRequest(new { Error = result.ErrorMessage });
        }
        return Ok(new { Id = result.RedirectId });
    }

    /// <summary>
    /// Full update of a contact method. All fields are replaced.
    /// Use PATCH for partial updates.
    /// </summary>
    /// <param name="id">The contact method GUID.</param>
    /// <param name="model">The complete contact method data.</param>
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] ContactMethodFormDto model)
    {
        model.Id = id;
        OperationResult result = await _contactMethodService.UpdateAsync(id, model);
        return !result.Success ? BadRequest(new { Error = result.ErrorMessage }) : NoContent();
    }

    /// <summary>
    /// Partial update of a contact method using JSON Merge Patch (RFC 7396).
    /// Only include the fields you want to change.
    /// </summary>
    /// <param name="id">The contact method GUID.</param>
    /// <param name="patch">A JSON object containing only the fields to update.</param>
    [HttpPatch("{id}")]
    public async Task<IActionResult> Patch(Guid id, [FromBody] JsonElement patch)
    {
        ContactMethodFormDto? existing = await _contactMethodService.GetFormAsync(id);
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

        OperationResult result = await _contactMethodService.UpdateAsync(id, existing);
        return !result.Success ? BadRequest(new { Error = result.ErrorMessage }) : NoContent();
    }

    /// <summary>
    /// Delete a contact method.
    /// </summary>
    /// <param name="id">The contact method GUID.</param>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        OperationResult result = await _contactMethodService.DeleteAsync(id);
        return !result.Success ? BadRequest(new { Error = result.ErrorMessage }) : NoContent();
    }
}