namespace Rvnx.CRM.Core.Interfaces
{
    public interface IFileValidationService
    {
        /// <summary>
        /// Validates if the file content matches the expected signature (magic numbers) for the given image extension.
        /// </summary>
        /// <param name="fileBytes">The raw file content.</param>
        /// <param name="extension">The file extension (e.g., ".jpg").</param>
        /// <returns>True if the signature is valid; otherwise, false.</returns>
        bool IsValidImageSignature(byte[] fileBytes, string extension);

        /// <summary>
        /// Checks if the given extension corresponds to a supported image type (jpg, png, gif, bmp, webp).
        /// </summary>
        /// <param name="extension">The file extension.</param>
        /// <returns>True if it is an image extension; otherwise, false.</returns>
        bool IsImageExtension(string extension);

        /// <summary>
        /// Validates if the file content matches the expected signature for any allowed file type.
        /// </summary>
        /// <param name="fileBytes">The raw file content.</param>
        /// <param name="extension">The file extension.</param>
        /// <returns>True if the signature is valid; otherwise, false.</returns>
        bool IsValidFileSignature(byte[] fileBytes, string extension);

        /// <summary>
        /// Checks if the file extension is in the allowed list for uploads.
        /// </summary>
        /// <param name="extension">The file extension.</param>
        /// <returns>True if allowed; otherwise, false.</returns>
        bool IsAllowedExtension(string extension);

        /// <summary>
        /// Checks if the file size is within the allowed limit (default 10MB).
        /// </summary>
        /// <param name="length">The file size in bytes.</param>
        /// <returns>True if the size is allowed; otherwise, false.</returns>
        bool IsAllowedFileSize(long length);

        /// <summary>
        /// Returns the MIME type associated with the given file extension.
        /// Defaults to "application/octet-stream" if unknown.
        /// </summary>
        /// <param name="extension">The file extension.</param>
        /// <returns>The MIME type string.</returns>
        string GetMimeType(string extension);
    }
}