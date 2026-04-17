using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rvnx.CRM.API.Helpers;
using Rvnx.CRM.Core.DTOs.Contact;
using Rvnx.CRM.Core.Interfaces;
using System.Text.Json;

namespace Rvnx.CRM.API.Controllers;

/// <summary>
/// Manages contacts — the central entity in the CRM. Most other resources belong to a contact.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ContactsController(
    IContactReadService contactReadService,
    IContactManagementService contactManagementService) : ControllerBase
{
    private readonly IContactReadService _contactReadService = contactReadService;
    private readonly IContactManagementService _contactManagementService = contactManagementService;

    /// <summary>
    /// List all contacts. Returns a flat array of contact summaries.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> List()
    {
        List<ContactDto> contacts = await _contactReadService.GetIndexDataAsync(false);
        return Ok(contacts);
    }

    /// <summary>
    /// Get a single contact by ID, including all editable fields.
    /// </summary>
    /// <param name="id">The contact GUID.</param>
    [HttpGet("{id}")]
    public async Task<IActionResult> Get(Guid id)
    {
        ContactFormDto? contact = await _contactReadService.GetContactFormAsync(id);
        return contact == null ? NotFound() : Ok(contact);
    }

    /// <summary>
    /// Create a new contact. FirstName is required.
    /// </summary>
    /// <param name="model">The contact data. Required field: firstName.</param>
    /// <returns>The new contact's ID.</returns>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ContactFormDto model)
    {
        ContactOperationResult result = await _contactManagementService.CreateContactAsync(model);
        if (!result.Success)
        {
            return BadRequest(new { result.Errors });
        }
        return CreatedAtAction(nameof(Get), new { id = result.ContactId }, new { Id = result.ContactId });
    }

    /// <summary>
    /// Full update of a contact. All fields are replaced — omitted fields are set to null.
    /// Use PATCH for partial updates.
    /// </summary>
    /// <param name="id">The contact GUID.</param>
    /// <param name="model">The complete contact data.</param>
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] ContactFormDto model)
    {
        model.Id = id;
        ContactOperationResult result = await _contactManagementService.UpdateContactAsync(id, model, null, null, null);
        return !result.Success ? result.IsNotFound ? NotFound() : BadRequest(new { result.Errors }) : NoContent();
    }

    /// <summary>
    /// Set an existing attachment as the contact's profile photo.
    /// </summary>
    /// <param name="id">The contact GUID.</param>
    /// <param name="attachmentId">The attachment GUID (must already be uploaded).</param>
    [HttpPost("{id}/photo/{attachmentId}")]
    public async Task<IActionResult> SetPhoto(Guid id, Guid attachmentId)
    {
        ContactOperationResult result = await _contactManagementService.SetAttachmentAsProfilePhotoAsync(id, attachmentId);
        return !result.Success ? result.IsNotFound ? NotFound() : BadRequest(new { result.Errors }) : NoContent();
    }

    /// <summary>
    /// Partial update of a contact using JSON Merge Patch (RFC 7396).
    /// Only include the fields you want to change. All other fields are preserved.
    /// </summary>
    /// <remarks>
    /// Example: To update only the gender, send: { "gender": "Male" }.
    /// To clear a field, send it as null: { "nickname": null }.
    /// </remarks>
    /// <param name="id">The contact GUID.</param>
    /// <param name="patch">A JSON object containing only the fields to update.</param>
    [HttpPatch("{id}")]
    public async Task<IActionResult> Patch(Guid id, [FromBody] JsonElement patch)
    {
        ContactFormDto? existing = await _contactReadService.GetContactFormAsync(id);
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

        ContactOperationResult result = await _contactManagementService.UpdateContactAsync(id, existing, null, null, null);
        return !result.Success ? result.IsNotFound ? NotFound() : BadRequest(new { result.Errors }) : NoContent();
    }

    /// <summary>
    /// Remove the contact's profile photo. The attachment itself is not deleted.
    /// </summary>
    /// <param name="id">The contact GUID.</param>
    [HttpDelete("{id}/photo")]
    public async Task<IActionResult> UnsetPhoto(Guid id)
    {
        ContactOperationResult result = await _contactManagementService.UnsetProfilePhotoAsync(id);
        return !result.Success ? result.IsNotFound ? NotFound() : BadRequest(new { result.Errors }) : NoContent();
    }

    /// <summary>
    /// Permanently delete a contact and all associated data.
    /// </summary>
    /// <param name="id">The contact GUID.</param>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _contactManagementService.DeleteContactAsync(id);
        return NoContent();
    }
}