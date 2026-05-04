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
    IContactManagementService contactManagementService,
    IContactImportService contactImportService,
    IContactExportService contactExportService,
    ICsvExportService csvExportService) : ControllerBase
{
    private readonly IContactReadService _contactReadService = contactReadService;
    private readonly IContactManagementService _contactManagementService = contactManagementService;
    private readonly IContactImportService _contactImportService = contactImportService;
    private readonly IContactExportService _contactExportService = contactExportService;
    private readonly ICsvExportService _csvExportService = csvExportService;

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
        return result.ToNoContentResult();
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
        return result.ToNoContentResult();
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

        IActionResult? validationFailure = JsonMergePatchHelper.ApplyAndValidate(existing, patch);
        if (validationFailure != null)
        {
            return validationFailure;
        }

        ContactOperationResult result = await _contactManagementService.UpdateContactAsync(id, existing, null, null, null);
        return result.ToNoContentResult();
    }

    /// <summary>
    /// Remove the contact's profile photo. The attachment itself is not deleted.
    /// </summary>
    /// <param name="id">The contact GUID.</param>
    [HttpDelete("{id}/photo")]
    public async Task<IActionResult> UnsetPhoto(Guid id)
    {
        ContactOperationResult result = await _contactManagementService.UnsetProfilePhotoAsync(id);
        return result.ToNoContentResult();
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

    /// <summary>
    /// Demote a full contact to a partial contact (sets the IsPartial flag).
    /// Fails if the contact still has dependent records that require a full profile.
    /// </summary>
    /// <param name="id">The contact GUID.</param>
    [HttpPost("{id}/demote")]
    public async Task<IActionResult> DemoteToPartial(Guid id)
    {
        ContactOperationResult result = await _contactManagementService.DemoteToPartialAsync(id);
        return result.ToNoContentResult();
    }

    /// <summary>
    /// Import contacts from a vCard (.vcf) file. Submit as multipart/form-data with a "file" field.
    /// Returns the count of added and skipped (duplicate) entries.
    /// </summary>
    /// <param name="file">The vCard file.</param>
    [HttpPost("import")]
    public async Task<IActionResult> Import(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { Error = "No file uploaded." });
        }

        await using Stream stream = file.OpenReadStream();
        ContactImportResult result = await _contactImportService.ImportFromVCardAsync(stream);
        return Ok(result);
    }

    /// <summary>
    /// Export a single contact as a vCard (.vcf) file.
    /// </summary>
    /// <param name="id">The contact GUID.</param>
    [HttpGet("{id}/export.vcf")]
    public async Task<IActionResult> ExportVCard(Guid id)
    {
        ContactExportResult result = await _contactExportService.ExportToVCardAsync(id);
        return result.FileContent.Length == 0
            ? NotFound()
            : File(result.FileContent, result.ContentType, result.FileName);
    }

    /// <summary>
    /// Export all contacts as an RFC 4180 CSV file. Includes flattened emails,
    /// phone numbers, primary address, and birthday.
    /// </summary>
    [HttpGet("export.csv")]
    public async Task<IActionResult> ExportCsv()
    {
        ContactExportResult result = await _csvExportService.ExportContactsAsync();
        return File(result.FileContent, result.ContentType, result.FileName);
    }

    /// <summary>
    /// Export all contacts as a ZIP archive of vCard (.vcf) files.
    /// </summary>
    [HttpGet("export.zip")]
    public async Task<IActionResult> ExportAllVCard(CancellationToken cancellationToken)
    {
        ContactExportResult result = await _contactExportService.ExportAllToVCardZipAsync(cancellationToken);
        return File(result.FileContent, result.ContentType, result.FileName);
    }
}
