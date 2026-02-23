using Microsoft.AspNetCore.Mvc;
using Rvnx.CRM.Core.DTOs.Base;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Web.Controllers.Base;
using System.Collections.Frozen;

namespace Rvnx.CRM.Web.Controllers
{
    public class AttachmentsController(IAttachmentService attachmentService) : AuthorizedController
    {
        private readonly IAttachmentService _attachmentService = attachmentService;
        private static readonly FrozenSet<string> ImageContentTypes = new[] { "image/jpeg", "image/png", "image/gif" }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(Guid entityId, string entityType, IFormFile file, string? returnUrl = null)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("File is empty.");
            }

            using MemoryStream ms = new();
            await file.CopyToAsync(ms);
            byte[] fileBytes = ms.ToArray();

            AttachmentOperationResult result = await _attachmentService.UploadAttachmentAsync(entityId, entityType, fileBytes, file.FileName, file.ContentType);

            if (result.Success)
            {
                return SafeRedirect(returnUrl);
            }

            if (result.IsNotFound)
            {
                return NotFound(string.Join("; ", result.Errors));
            }

            return BadRequest(string.Join("; ", result.Errors));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id, string? returnUrl = null)
        {
            AttachmentOperationResult result = await _attachmentService.DeleteAttachmentAsync(id);

            if (result.IsNotFound && result.Errors.Any(e => e.Contains("partial contact", StringComparison.OrdinalIgnoreCase)))
            {
                return NotFound();
            }

            return SafeRedirect(returnUrl);
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
            if (dto == null)
            {
                return NotFound();
            }

            return File(dto.Content, dto.ContentType, dto.FileName);
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

            return ImageContentTypes.Contains(dto.ContentType)
                ? File(dto.Content, dto.ContentType)
                : File(dto.Content, dto.ContentType, dto.FileName);
        }
    }
}
