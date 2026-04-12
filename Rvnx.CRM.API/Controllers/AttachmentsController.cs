using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rvnx.CRM.Core.Interfaces;

namespace Rvnx.CRM.API.Controllers;

/// <summary>
/// Manages file attachments for contacts. Supports upload (multipart/form-data),
/// download, and thumbnail generation for images.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AttachmentsController(IAttachmentService attachmentService, IThumbnailService thumbnailService) : ControllerBase
{
    private readonly IAttachmentService _attachmentService = attachmentService;
    private readonly IThumbnailService _thumbnailService = thumbnailService;

    /// <summary>
    /// List all attachments for a specific contact.
    /// </summary>
    /// <param name="contactId">The contact GUID.</param>
    [HttpGet("contact/{contactId}")]
    public async Task<IActionResult> ListByContact(Guid contactId)
    {
        List<Core.DTOs.Base.AttachmentDto> attachments = await _attachmentService.GetByContactAsync(contactId);
        return Ok(attachments);
    }

    /// <summary>
    /// Upload a file attachment to a contact. Use multipart/form-data with a "file" field.
    /// </summary>
    /// <param name="contactId">The contact GUID.</param>
    /// <param name="file">The file to upload.</param>
    /// <returns>The new attachment's ID.</returns>
    [HttpPost("contact/{contactId}")]
    public async Task<IActionResult> Upload(Guid contactId, IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("No file uploaded.");
        }

        using MemoryStream stream = new();
        await file.CopyToAsync(stream);
        byte[] content = stream.ToArray();

        Core.DTOs.Base.AttachmentOperationResult result = await _attachmentService.UploadAttachmentAsync(contactId, "Person", content, file.FileName);

        if (!result.Success)
        {
            return result.IsNotFound ? NotFound() : BadRequest(new { result.Errors });
        }

        return Ok(new { Id = result.AttachmentId });
    }

    /// <summary>
    /// Download an attachment's file content. Returns the binary file with its original Content-Type.
    /// </summary>
    /// <param name="id">The attachment GUID.</param>
    [HttpGet("{id}/download")]
    public async Task<IActionResult> Download(Guid id)
    {
        Core.DTOs.Base.AttachmentDto? attachment = await _attachmentService.GetAttachmentAsync(id);
        if (attachment == null)
        {
            return NotFound();
        }

        Core.DTOs.Base.AttachmentContentDto? content = await _attachmentService.GetAttachmentContentAsync(id);
        return content == null ? NotFound() : File(content.Content, content.ContentType, attachment.FileName);
    }

    /// <summary>
    /// Get a JPEG thumbnail of an image attachment. Falls back to the full file if
    /// thumbnail generation fails. Optional maxWidth and maxHeight parameters control size.
    /// </summary>
    /// <param name="id">The attachment GUID.</param>
    /// <param name="maxWidth">Maximum thumbnail width in pixels.</param>
    /// <param name="maxHeight">Maximum thumbnail height in pixels.</param>
    [HttpGet("{id}/thumbnail")]
    public async Task<IActionResult> Thumbnail(Guid id, int? maxWidth = null, int? maxHeight = null)
    {
        Core.DTOs.Base.AttachmentContentDto? dto = await _attachmentService.GetAttachmentContentAsync(id);
        if (dto == null)
        {
            return NotFound();
        }

        byte[]? thumbnail = await _thumbnailService.GetOrCreateThumbnailAsync(id, dto.Content, dto.ContentType, maxWidth, maxHeight);

        if (thumbnail != null)
        {
            Response.Headers.LastModified = dto.LastChangedDate.ToString("R");
            Response.Headers.CacheControl = "public, max-age=604800";
            return File(thumbnail, "image/jpeg");
        }

        // Thumbnail generation failed or not an image — fall back to full file
        // Short cache so the caller retries rather than caching the failure permanently
        Response.Headers.LastModified = dto.LastChangedDate.ToString("R");
        Response.Headers.CacheControl = "public, max-age=3600";
        return File(dto.Content, dto.ContentType, dto.FileName);
    }

    /// <summary>
    /// Delete an attachment.
    /// </summary>
    /// <param name="id">The attachment GUID.</param>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        Core.DTOs.Base.AttachmentOperationResult result = await _attachmentService.DeleteAttachmentAsync(id);
        return !result.Success ? result.IsNotFound ? NotFound() : BadRequest(new { result.Errors }) : NoContent();
    }
}
