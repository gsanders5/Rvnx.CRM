using Rvnx.CRM.Core.DTOs.Base;

namespace Rvnx.CRM.Core.Interfaces;

public interface IImmichService
{
    bool IsEnabled { get; }

    Task<IReadOnlyList<ImmichOptionDto>> SearchPeopleAsync(string? query, CancellationToken ct);

    Task<IReadOnlyList<ImmichOptionDto>> SearchTagsAsync(string? query, CancellationToken ct);

    Task<IReadOnlyList<ImmichAssetDto>> GetAssetsAsync(Guid? personId, Guid? tagId, int maxResults, CancellationToken ct);

    Task<ImmichMediaPayload?> GetThumbnailAsync(Guid assetId, CancellationToken ct);

    Task<ImmichMediaPayload?> GetOriginalAsync(Guid assetId, CancellationToken ct);
}

// Response owns the connection; caller is responsible for disposing it after the stream has been written.
public sealed record ImmichMediaPayload(HttpResponseMessage Response, Stream Content, string ContentType);
