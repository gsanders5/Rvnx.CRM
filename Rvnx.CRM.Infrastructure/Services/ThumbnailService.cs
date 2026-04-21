using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Rvnx.CRM.Core.Interfaces;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;

namespace Rvnx.CRM.Infrastructure.Services;

public class ThumbnailService(IMemoryCache cache, ILogger<ThumbnailService> logger) : IThumbnailService
{
    private const int DefaultMaxSize = 200;
    private const int MaxAllowedSize = 1200;
    private const int JpegQuality = 75;
    private static readonly TimeSpan CacheExpiration = TimeSpan.FromHours(24);

    private readonly IMemoryCache _cache = cache;
    private readonly ILogger<ThumbnailService> _logger = logger;

    private static readonly Action<ILogger, Guid, int?, int?, Exception?> LogThumbnailGenerationFailed =
        LoggerMessage.Define<Guid, int?, int?>(LogLevel.Warning, new EventId(1, nameof(LogThumbnailGenerationFailed)),
            "Failed to generate thumbnail for attachment {AttachmentId} at maxWidth={MaxWidth}, maxHeight={MaxHeight}");

    public async Task<byte[]?> GetOrCreateThumbnailAsync(Guid attachmentId, byte[] imageContent, string contentType, int? maxWidth, int? maxHeight)
    {
        if (!contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        maxWidth = maxWidth.HasValue ? Math.Clamp(maxWidth.Value, 1, MaxAllowedSize) : null;
        maxHeight = maxHeight.HasValue ? Math.Clamp(maxHeight.Value, 1, MaxAllowedSize) : null;

        ThumbnailCacheKey cacheKey = new(attachmentId, maxWidth, maxHeight);
        if (_cache.TryGetValue(cacheKey, out byte[]? cached))
        {
            return cached;
        }

        byte[]? thumbnail = await GenerateThumbnailAsync(attachmentId, imageContent, maxWidth, maxHeight);

        if (thumbnail != null)
        {
            MemoryCacheEntryOptions entryOptions = new MemoryCacheEntryOptions().SetAbsoluteExpiration(CacheExpiration).SetSize(thumbnail.Length);
            _cache.Set(cacheKey, thumbnail, entryOptions);
        }

        return thumbnail;
    }

    private async Task<byte[]?> GenerateThumbnailAsync(Guid attachmentId, byte[] imageContent, int? maxWidth, int? maxHeight)
    {
        try
        {
            int targetWidth = maxWidth ?? (maxHeight.HasValue ? 0 : DefaultMaxSize);
            int targetHeight = maxHeight ?? (maxWidth.HasValue ? 0 : DefaultMaxSize);

            using MemoryStream inputStream = new(imageContent);
            using Image image = await Image.LoadAsync(inputStream);

            image.Mutate(x => x.Resize(new ResizeOptions
            {
                Size = new Size(targetWidth, targetHeight),
                Mode = ResizeMode.Max
            }));

            using MemoryStream outputStream = new();
            await image.SaveAsJpegAsync(outputStream, new JpegEncoder { Quality = JpegQuality });
            return outputStream.ToArray();
        }
        catch (Exception ex)
        {
            LogThumbnailGenerationFailed(_logger, attachmentId, maxWidth, maxHeight, ex);
            return null;
        }
    }

    private readonly record struct ThumbnailCacheKey(Guid AttachmentId, int? MaxWidth, int? MaxHeight);
}
