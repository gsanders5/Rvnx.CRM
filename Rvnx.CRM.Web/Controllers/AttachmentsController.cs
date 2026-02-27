using FileTypeChecker.Web.Attributes;
using Microsoft.AspNetCore.Mvc;
using Rvnx.CRM.Core.DTOs.Base;
using Rvnx.CRM.Core.DTOs.Contact;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Web.Controllers.Base;
using System.Collections.Frozen;

namespace Rvnx.CRM.Web.Controllers
{
    public class AttachmentsController(IAttachmentService attachmentService, IFileValidationService fileValidationService) : AuthorizedController
    {
        private readonly IAttachmentService _attachmentService = attachmentService;
        private readonly IFileValidationService _fileValidationService = fileValidationService;
        private static readonly FrozenSet<string> ImageContentTypes = new[] { "image/jpeg", "image/png", "image/gif" }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(Guid entityId, string entityType, [ForbidExecutables] IFormFile file, string? returnUrl = null)
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

            AttachmentOperationResult result = await _attachmentService.UploadAttachmentAsync(entityId, entityType, fileBytes, file.FileName);

            return result.Success
                ? SafeRedirect(returnUrl)
                : result.IsNotFound ? NotFound(string.Join("; ", result.Errors)) : BadRequest(string.Join("; ", result.Errors));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id, string? returnUrl = null)
        {
            AttachmentOperationResult result = await _attachmentService.DeleteAttachmentAsync(id);

            return result.IsNotFound && result.Errors.Any(e => e.Contains("partial contact", StringComparison.OrdinalIgnoreCase))
                ? NotFound()
                : SafeRedirect(returnUrl);
        }

        [HttpPost("Attachments/{id}/SetAsProfilePhoto")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetAsProfilePhoto(Guid id, [FromServices] IContactManagementService contactManagementService, string? returnUrl = null)
        {
            AttachmentDto? attachment = await _attachmentService.GetAttachmentAsync(id);
            if (attachment == null)
            {
                return NotFound();
            }

            ContactOperationResult result = await contactManagementService.SetAttachmentAsProfilePhotoAsync(attachment.EntityId, id);

            return result.Success ? SafeRedirect(returnUrl) : result.IsNotFound ? NotFound() : BadRequest(string.Join("; ", result.Errors));
        }

        private IActionResult SafeRedirect(string? returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return LocalRedirect(returnUrl);
            }

            string referer = Request.Headers.Referer.ToString();
            return Uri.TryCreate(referer, UriKind.Absolute, out Uri? uri) && string.Equals(uri.Host, Request.Host.Host, StringComparison.OrdinalIgnoreCase)
                ? Redirect(referer)
                : RedirectToAction("Index", "Home");
        }

        public async Task<IActionResult> Download(Guid id)
        {
            AttachmentContentDto? dto = await _attachmentService.GetAttachmentContentAsync(id);
            return dto == null ? NotFound() : File(dto.Content, dto.ContentType, dto.FileName);
        }

        public async Task<IActionResult> View(Guid id)
        {
            AttachmentContentDto? dto = await _attachmentService.GetAttachmentContentAsync(id);
            if (dto == null)
            {
                return NotFound();
            }

            if (!string.IsNullOrEmpty(Request.Headers.IfModifiedSince))
            {
                if (DateTime.TryParse(Request.Headers.IfModifiedSince,
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.AdjustToUniversal | System.Globalization.DateTimeStyles.AssumeUniversal,
                    out DateTime ifModifiedSince))
                {
                    // Truncate milliseconds as HTTP headers don't support them
                    if (ifModifiedSince >= dto.LastChangedDate.AddTicks(-(dto.LastChangedDate.Ticks % TimeSpan.TicksPerSecond)))
                    {
                        return StatusCode(304);
                    }
                }
            }

            Response.Headers.LastModified = dto.LastChangedDate.ToString("R");
            Response.Headers.CacheControl = "public, max-age=31536000";

            return IsImage(dto.ContentType)
                ? File(dto.Content, dto.ContentType)
                : File(dto.Content, dto.ContentType, dto.FileName);
        }

        private static bool IsImage(string contentType)
        {
            return ImageContentTypes.Contains(contentType);
        }
    }
}
