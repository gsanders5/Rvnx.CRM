using FileTypeChecker.Web.Attributes;
using Microsoft.AspNetCore.Mvc;
using Rvnx.CRM.Core.DTOs.Base;
using Rvnx.CRM.Core.DTOs.Contact;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Web.Controllers.Base;

namespace Rvnx.CRM.Web.Controllers;

public class AttachmentsController(
    IAttachmentService attachmentService,
    IFileValidationService fileValidationService,
    IThumbnailService thumbnailService) : AuthorizedController
{
    private readonly IAttachmentService _attachmentService = attachmentService;
    private readonly IFileValidationService _fileValidationService = fileValidationService;
    private readonly IThumbnailService _thumbnailService = thumbnailService;

    [HttpPost]
    public async Task<IActionResult> Upload(Guid contactId, [ForbidExecutables] IFormFile file,
        string? returnUrl = null)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("File is empty.");
        }

        if (!ModelState.IsValid)
        {
            return BadRequest("File type validation failed.");
        }

        if (!_fileValidationService.IsAllowedFileSize(file.Length))
        {
            return BadRequest("File is too large.");
        }

        if (!_fileValidationService.IsAllowedExtension(Path.GetExtension(file.FileName)))
        {
            return BadRequest("File type not allowed.");
        }

        using MemoryStream ms = new();
        await file.CopyToAsync(ms);
        byte[] fileBytes = ms.ToArray();

        AttachmentOperationResult result =
            await _attachmentService.UploadAttachmentAsync(contactId, fileBytes, file.FileName);

        return result.Success
            ? SafeRedirect(returnUrl)
            : result.IsNotFound
                ? NotFound(string.Join("; ", result.Errors))
                : BadRequest(string.Join("; ", result.Errors));
    }

    [HttpPost]
    public async Task<IActionResult> Delete(Guid id, string? returnUrl = null)
    {
        AttachmentOperationResult result = await _attachmentService.DeleteAttachmentAsync(id);

        return result.IsNotFound &&
               result.Errors.Any(e => e.Contains("partial contact", StringComparison.OrdinalIgnoreCase))
            ? NotFound()
            : SafeRedirect(returnUrl);
    }

    [HttpPost("Attachments/{id}/SetAsProfilePhoto")]
    public async Task<IActionResult> SetAsProfilePhoto(Guid id,
        [FromServices] IContactManagementService contactManagementService, string? returnUrl = null)
    {
        AttachmentDto? attachment = await _attachmentService.GetAttachmentAsync(id);
        if (attachment == null)
        {
            return NotFound();
        }

        ContactOperationResult result =
            await contactManagementService.SetAttachmentAsProfilePhotoAsync(attachment.ContactId, id);

        return result.Success ? SafeRedirect(returnUrl) :
            result.IsNotFound ? NotFound() : BadRequest(string.Join("; ", result.Errors));
    }

    [HttpGet]
    public async Task<IActionResult> Download(Guid id)
    {
        AttachmentContentDto? dto = await _attachmentService.GetAttachmentContentAsync(id);
        return dto == null ? NotFound() : File(dto.Content, dto.ContentType, dto.FileName);
    }

    [HttpGet]
    public async Task<IActionResult> View(Guid id)
    {
        AttachmentContentDto? dto = await _attachmentService.GetAttachmentContentAsync(id);
        if (dto == null)
        {
            return NotFound();
        }

        if (IsNotModified(dto.LastChangedDate))
        {
            return StatusCode(304);
        }

        Response.Headers.LastModified = dto.LastChangedDate.ToString("R");
        Response.Headers.CacheControl = "public, max-age=31536000";

        return _fileValidationService.IsImageContentType(dto.ContentType)
            ? File(dto.Content, dto.ContentType)
            : File(dto.Content, dto.ContentType, dto.FileName);
    }

    [HttpGet]
    public async Task<IActionResult> Thumbnail(Guid id, int? maxWidth = null, int? maxHeight = null)
    {
        AttachmentContentDto? dto = await _attachmentService.GetAttachmentContentAsync(id);
        if (dto == null)
        {
            return NotFound();
        }

        if (IsNotModified(dto.LastChangedDate))
        {
            return StatusCode(304);
        }

        byte[]? thumbnail = await _thumbnailService.GetOrCreateThumbnailAsync(
            id, dto.Content, dto.ContentType, maxWidth, maxHeight);

        if (thumbnail != null)
        {
            Response.Headers.LastModified = dto.LastChangedDate.ToString("R");
            Response.Headers.CacheControl = "public, max-age=604800";
            return File(thumbnail, "image/jpeg");
        }

        // Thumbnail generation failed or not an image — fall back to full file
        // Short cache so the browser retries rather than caching the failure permanently
        Response.Headers.LastModified = dto.LastChangedDate.ToString("R");
        Response.Headers.CacheControl = "public, max-age=3600";
        return _fileValidationService.IsImageContentType(dto.ContentType)
            ? File(dto.Content, dto.ContentType)
            : File(dto.Content, dto.ContentType, dto.FileName);
    }

    private bool IsNotModified(DateTime lastChangedDate)
    {
        if (string.IsNullOrEmpty(Request.Headers.IfModifiedSince))
        {
            return false;
        }

        if (DateTime.TryParse(Request.Headers.IfModifiedSince,
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.AdjustToUniversal |
                System.Globalization.DateTimeStyles.AssumeUniversal,
                out DateTime ifModifiedSince))
        {
            // Truncate milliseconds as HTTP headers don't support them
            return ifModifiedSince >= lastChangedDate.AddTicks(-(lastChangedDate.Ticks % TimeSpan.TicksPerSecond));
        }

        return false;
    }
}
