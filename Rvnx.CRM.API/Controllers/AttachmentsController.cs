using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rvnx.CRM.Core.Interfaces;

namespace Rvnx.CRM.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AttachmentsController(IAttachmentService attachmentService, IThumbnailService thumbnailService) : ControllerBase
{
    private readonly IAttachmentService _attachmentService = attachmentService;
    private readonly IThumbnailService _thumbnailService = thumbnailService;

    [HttpGet("contact/{contactId}")]
    public async Task<IActionResult> ListByContact(Guid contactId)
    {
        List<Core.DTOs.Base.AttachmentDto> attachments = await _attachmentService.GetByContactAsync(contactId);
        return Ok(attachments);
    }

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

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        Core.DTOs.Base.AttachmentOperationResult result = await _attachmentService.DeleteAttachmentAsync(id);
        return !result.Success ? result.IsNotFound ? NotFound() : BadRequest(new { result.Errors }) : NoContent();
    }
}