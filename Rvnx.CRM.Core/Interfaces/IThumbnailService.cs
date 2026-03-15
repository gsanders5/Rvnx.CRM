namespace Rvnx.CRM.Core.Interfaces;

public interface IThumbnailService
{
    /// <summary>
    /// Returns a thumbnail for the given image content, generating and caching it if not already cached.
    /// At least one of maxWidth or maxHeight must be provided; if both are null, a default size is used.
    /// Returns null if the content is not a supported image type or if generation fails.
    /// </summary>
    /// <param name="attachmentId">Used as part of the cache key.</param>
    /// <param name="imageContent">The raw image bytes.</param>
    /// <param name="contentType">The MIME type of the source image.</param>
    /// <param name="maxWidth">Optional maximum width in pixels. Aspect ratio is preserved.</param>
    /// <param name="maxHeight">Optional maximum height in pixels. Aspect ratio is preserved.</param>
    /// <returns>JPEG thumbnail bytes, or null on failure.</returns>
    Task<byte[]?> GetOrCreateThumbnailAsync(Guid attachmentId, byte[] imageContent, string contentType, int? maxWidth, int? maxHeight);
}