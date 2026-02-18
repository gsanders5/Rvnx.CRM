using Microsoft.AspNetCore.Mvc;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models.Base;
using Rvnx.CRM.Web.Controllers.Base;

namespace Rvnx.CRM.Web.Controllers
{
    public class AttachmentsController : AuthorizedController
    {
        private readonly IRepository _repository;
        private readonly IFileValidationService _fileValidationService;
        private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".pdf", ".txt", ".doc", ".docx", ".xls", ".xlsx" };
        private static readonly string[] ImageContentTypes = { "image/jpeg", "image/png", "image/gif" };

        public AttachmentsController(IRepository repository, IFileValidationService fileValidationService)
        {
            _repository = repository;
            _fileValidationService = fileValidationService;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(Guid entityId, string entityType, IFormFile file, string? returnUrl = null)
        {
            if (file == null || file.Length == 0) return BadRequest("File is empty.");

            string extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(extension))
            {
                return BadRequest("File type not allowed.");
            }

            using MemoryStream ms = new();
            await file.CopyToAsync(ms);
            byte[] fileBytes = ms.ToArray();

            if (_fileValidationService.IsImageExtension(extension) && !_fileValidationService.IsValidImageSignature(fileBytes, extension))
            {
                return BadRequest("Invalid file signature.");
            }

            Attachment attachment = new()
            {
                Id = Guid.NewGuid(),
                EntityId = entityId,
                EntityType = entityType,
                AttachmentType = "General",
                ContentType = file.ContentType,
                FileName = file.FileName,
                AttachmentContent = new AttachmentContent
                {
                    Content = fileBytes
                }
            };

            await _repository.AddAsync(attachment);
            await _repository.SaveChangesAsync();

            return SafeRedirect(returnUrl);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id, string? returnUrl = null)
        {
            Attachment? attachment = await _repository.GetByIdAsync<Attachment>(id);
            if (attachment != null)
            {
                await _repository.DeleteAsync<Attachment>(id);
                await _repository.SaveChangesAsync();
            }
            return SafeRedirect(returnUrl);
        }

        private IActionResult SafeRedirect(string? returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return LocalRedirect(returnUrl);
            }

            string referer = Request.Headers["Referer"].ToString();
            if (Uri.TryCreate(referer, UriKind.Absolute, out Uri? uri) && string.Equals(uri.Host, Request.Host.Host, StringComparison.OrdinalIgnoreCase))
            {
                return Redirect(referer);
            }

            return RedirectToAction("Index", "Home");
        }

        public async Task<IActionResult> Download(Guid id)
        {
            Attachment? attachment = await _repository.GetByIdWithIncludesAsync<Attachment>(id, "AttachmentContent");
            if (attachment == null || attachment.AttachmentContent == null) return NotFound();

            // Force download for everything except specific safe types if needed,
            // but 'File' result with filename argument usually sets Content-Disposition to attachment.
            return File(attachment.AttachmentContent.Content, attachment.ContentType, attachment.FileName);
        }

        public async Task<IActionResult> View(Guid id)
        {
            // 1. Fetch metadata only (fast)
            Attachment? attachment = await _repository.GetByIdAsync<Attachment>(id);
            if (attachment == null) return NotFound();

            // 2. Check Cache
            if (!string.IsNullOrEmpty(Request.Headers["If-Modified-Since"]))
            {
                if (DateTime.TryParse(Request.Headers["If-Modified-Since"],
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.AdjustToUniversal | System.Globalization.DateTimeStyles.AssumeUniversal,
                    out DateTime ifModifiedSince))
                {
                    // Truncate milliseconds as HTTP headers don't support them
                    if (ifModifiedSince >= attachment.LastChangedDate.AddTicks(-(attachment.LastChangedDate.Ticks % TimeSpan.TicksPerSecond)))
                    {
                        return StatusCode(304);
                    }
                }
            }

            // 3. Set Cache Headers
            Response.Headers["Last-Modified"] = attachment.LastChangedDate.ToString("R");
            Response.Headers["Cache-Control"] = "public, max-age=31536000";

            // 4. Fetch content (if not cached)
            attachment = await _repository.GetByIdWithIncludesAsync<Attachment>(id, "AttachmentContent");
            if (attachment == null || attachment.AttachmentContent == null) return NotFound();

            // Only allow inline viewing for safe image types
            if (ImageContentTypes.Contains(attachment.ContentType))
            {
                return File(attachment.AttachmentContent.Content, attachment.ContentType);
            }

            // Otherwise force download
            return File(attachment.AttachmentContent.Content, attachment.ContentType, attachment.FileName);
        }

    }
}
