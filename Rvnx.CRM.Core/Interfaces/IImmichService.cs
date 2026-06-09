using Rvnx.CRM.Core.DTOs.Base;

namespace Rvnx.CRM.Core.Interfaces;

public interface IImmichService
{
    // Async because the answer depends on the current group's settings row in the database.
    Task<bool> IsEnabledAsync(CancellationToken ct);

    // Web UI base (e.g. "https://immich.example.com") derived from the group's API BaseUrl;
    // null when Immich isn't configured for the current group.
    Task<string?> GetWebBaseUrlAsync(CancellationToken ct);

    Task<IReadOnlyList<ImmichOptionDto>> GetAllPeopleAsync(CancellationToken ct);

    Task<IReadOnlyList<ImmichOptionDto>> GetAllTagsAsync(CancellationToken ct);

    Task<IReadOnlyList<ImmichAssetDto>> GetAssetsAsync(Guid? personId, Guid? tagId, int maxResults, CancellationToken ct);

    Task<ImmichMediaPayload?> GetThumbnailAsync(Guid assetId, CancellationToken ct);

    Task<ImmichMediaPayload?> GetOriginalAsync(Guid assetId, CancellationToken ct);
}

// Response owns the connection; caller is responsible for disposing it after the stream has been written.
public sealed record ImmichMediaPayload(HttpResponseMessage Response, Stream Content, string ContentType)
{
    public string DefaultExtension => ContentType switch
    {
        var t when t.StartsWith("image/png", StringComparison.OrdinalIgnoreCase) => ".png",
        var t when t.StartsWith("image/gif", StringComparison.OrdinalIgnoreCase) => ".gif",
        var t when t.StartsWith("image/webp", StringComparison.OrdinalIgnoreCase) => ".webp",
        _ => ".jpg",
    };
}
