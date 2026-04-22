using Microsoft.AspNetCore.Mvc;
using Rvnx.CRM.Core.DTOs.Base;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Web.Controllers.Base;

namespace Rvnx.CRM.Web.Controllers;

public class ImmichController(IImmichService immichService) : AuthorizedController
{
    private readonly IImmichService _immichService = immichService;

    private const int MaxAssets = 24;

    [HttpGet("Immich/People")]
    public async Task<IActionResult> People(string? q, CancellationToken ct)
    {
        IReadOnlyList<ImmichOptionDto> results = await _immichService.SearchPeopleAsync(q, ct);
        return Json(new { results });
    }

    [HttpGet("Immich/Tags")]
    public async Task<IActionResult> Tags(string? q, CancellationToken ct)
    {
        IReadOnlyList<ImmichOptionDto> results = await _immichService.SearchTagsAsync(q, ct);
        return Json(new { results });
    }

    [HttpGet("Immich/Gallery")]
    public async Task<IActionResult> Gallery(Guid? personId, Guid? tagId, CancellationToken ct)
    {
        IReadOnlyList<ImmichAssetDto> assets = _immichService.IsEnabled
            ? await _immichService.GetAssetsAsync(personId, tagId, MaxAssets, ct)
            : [];
        return PartialView("_ImmichGallery", assets);
    }

    [HttpGet("Immich/Thumbnail/{assetId:guid}")]
    public Task<IActionResult> Thumbnail(Guid assetId, CancellationToken ct)
        => ProxyMedia(() => _immichService.GetThumbnailAsync(assetId, ct));

    [HttpGet("Immich/Original/{assetId:guid}")]
    public Task<IActionResult> Original(Guid assetId, CancellationToken ct)
        => ProxyMedia(() => _immichService.GetOriginalAsync(assetId, ct));

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
