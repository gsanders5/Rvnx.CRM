using Microsoft.AspNetCore.Mvc;
using Rvnx.CRM.Core.DTOs.Base;
using Rvnx.CRM.Core.DTOs.Contact;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Web.Controllers.Base;
using Rvnx.CRM.Web.ViewModels.Immich;
using System.Buffers;

namespace Rvnx.CRM.Web.Controllers;

public class ImmichController(
    IImmichService immichService,
    IAttachmentService attachmentService,
    IContactManagementService contactManagementService,
    IFileValidationService fileValidationService) : AuthorizedController
{
    private readonly IImmichService _immichService = immichService;
    private readonly IAttachmentService _attachmentService = attachmentService;
    private readonly IContactManagementService _contactManagementService = contactManagementService;
    private readonly IFileValidationService _fileValidationService = fileValidationService;

    private const int MaxAssets = 24;

    [HttpGet("Immich/Gallery")]
    public async Task<IActionResult> Gallery([FromQuery] ImmichGalleryRequest request, CancellationToken ct)
    {
        IReadOnlyList<ImmichAssetDto> assets =
            await _immichService.GetAssetsAsync(request.PersonId, request.TagId, MaxAssets, ct);

        ImmichGalleryViewModel vm = new()
        {
            ContactId = request.ContactId,
            PersonId = request.PersonId,
            PersonName = request.PersonName,
            TagId = request.TagId,
            TagValue = request.TagValue,
            WebBaseUrl = await _immichService.GetWebBaseUrlAsync(ct),
            Assets = assets,
        };

        return PartialView("_ImmichGallery", vm);
    }

    [HttpGet("Immich/Thumbnail/{assetId:guid}")]
    public Task<IActionResult> Thumbnail(Guid assetId, CancellationToken ct)
    {
        return ProxyMedia(() => _immichService.GetThumbnailAsync(assetId, ct));
    }

    [HttpGet("Immich/Original/{assetId:guid}")]
    public Task<IActionResult> Original(Guid assetId, CancellationToken ct)
    {
        return ProxyMedia(() => _immichService.GetOriginalAsync(assetId, ct));
    }

    [HttpPost("Immich/SetAsProfilePhoto")]
    public async Task<IActionResult> SetAsProfilePhoto(
        Guid contactId,
        Guid assetId,
        string? fileName,
        string? returnUrl,
        CancellationToken ct)
    {
        ImmichMediaPayload? media = await _immichService.GetOriginalAsync(assetId, ct);
        if (media == null)
        {
            return NotFound();
        }

        using (media.Response)
        {
            long? declaredLength = media.Response.Content.Headers.ContentLength;
            if (declaredLength is long len && !_fileValidationService.IsAllowedFileSize(len))
            {
                return BadRequest("File is too large.");
            }

            // IsAllowedFileSize already caps well below int.MaxValue.
            using MemoryStream ms = declaredLength is long capacity ? new((int)capacity) : new();

            // 🛡️ Sentinel: Prevent memory exhaustion DoS if Content-Length is missing or spoofed
            byte[] buffer = ArrayPool<byte>.Shared.Rent(81920);
            try
            {
                int bytesRead;
                while ((bytesRead = await media.Content.ReadAsync(buffer, ct)) > 0)
                {
                    ms.Write(buffer, 0, bytesRead);
                    if (!_fileValidationService.IsAllowedFileSize(ms.Length))
                    {
                        return BadRequest("File is too large.");
                    }
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
            byte[] bytes = ms.ToArray();

            string effectiveFileName = string.IsNullOrWhiteSpace(fileName)
                ? $"immich-{assetId}{media.DefaultExtension}"
                : fileName;

            AttachmentOperationResult upload = await _attachmentService.UploadAttachmentAsync(
                contactId, bytes, effectiveFileName);

            if (!upload.Success || upload.AttachmentId is null)
            {
                return upload.IsNotFound
                    ? NotFound()
                    : BadRequest(string.Join("; ", upload.Errors));
            }

            ContactOperationResult set = await _contactManagementService
                .SetAttachmentAsProfilePhotoAsync(contactId, upload.AttachmentId.Value);

            return !set.Success
                ? set.IsNotFound
                    ? NotFound()
                    : BadRequest(string.Join("; ", set.Errors))
                : SafeRedirect(returnUrl);
        }
    }

    private async Task<IActionResult> ProxyMedia(Func<Task<ImmichMediaPayload?>> fetch)
    {
        ImmichMediaPayload? media = await fetch();
        if (media == null)
        {
            return NotFound();
        }

        // Stream is consumed by FileStreamResult; response envelope (headers, connection) is released when the request ends.
        Response.RegisterForDispose(media.Response);
        Response.Headers.CacheControl = "private, max-age=3600";
        return File(media.Content, media.ContentType);
    }
}
