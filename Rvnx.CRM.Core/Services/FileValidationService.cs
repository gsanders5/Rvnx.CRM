using FileTypeChecker;
using FileTypeChecker.Extensions;
using Rvnx.CRM.Core.Interfaces;

namespace Rvnx.CRM.Core.Services
{
    public class FileValidationService : IFileValidationService
    {
        private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png", ".gif", ".pdf",
            ".txt", ".doc", ".docx", ".xls", ".xlsx", ".vcf"
        };

        private static readonly HashSet<string> ImageExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png", ".gif"
        };

        private static readonly Dictionary<string, string> MimeTypeMap = new(StringComparer.OrdinalIgnoreCase)
        {
            { ".jpg", "image/jpeg" },
            { ".jpeg", "image/jpeg" },
            { ".png", "image/png" },
            { ".gif", "image/gif" },
            { ".pdf", "application/pdf" },
            { ".txt", "text/plain" },
            { ".doc", "application/msword" },
            { ".docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document" },
            { ".xls", "application/vnd.ms-excel" },
            { ".xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" },
            { ".vcf", "text/vcard" }
        };

        private const long MaxFileSize = 30 * 1024 * 1024; // 30 MB

        public bool IsAllowedExtension(string extension)
        {
            return !string.IsNullOrEmpty(extension) && AllowedExtensions.Contains(extension);
        }

        public bool IsAllowedFileSize(long length)
        {
            return length is > 0 and <= MaxFileSize;
        }

        public bool IsImageExtension(string? extension)
        {
            return !string.IsNullOrEmpty(extension) && ImageExtensions.Contains(extension);
        }

        public bool IsValidImageSignature(byte[] fileBytes, string? extension)
        {
            if (fileBytes == null || fileBytes.Length == 0 || string.IsNullOrEmpty(extension))
            {
                return false;
            }

            if (!IsImageExtension(extension))
            {
                return false;
            }

            try
            {
                using MemoryStream stream = new(fileBytes);
                if (!stream.IsImage())
                {
                    return false;
                }

                stream.Position = 0;
                var fileType = FileTypeValidator.GetFileType(stream);
                string expectedMimeType = GetMimeType(extension);

                return fileType != null && string.Equals(expectedMimeType, fileType.MimeType, StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception)
            {
                // File.TypeChecker might throw if it can't read enough bytes or encounters an error
                return false;
            }
        }

        public bool IsValidFileSignature(byte[] fileBytes, string extension)
        {
            if (fileBytes == null || fileBytes.Length == 0 || string.IsNullOrEmpty(extension))
            {
                return false;
            }

            // .txt and .vcf have no magic number signature.
            // File.TypeChecker will not recognise these as known types.
            // Extension validation is the only applicable check for these formats.
            if (string.Equals(extension, ".txt", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(extension, ".vcf", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            try
            {
                using MemoryStream stream = new(fileBytes);
                if (!FileTypeValidator.IsTypeRecognizable(stream))
                {
                    return false;
                }

                stream.Position = 0;
                var fileType = FileTypeValidator.GetFileType(stream);
                string expectedMimeType = GetMimeType(extension);

                return fileType != null && string.Equals(expectedMimeType, fileType.MimeType, StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        public string GetMimeType(string extension)
        {
            return string.IsNullOrEmpty(extension)
                ? "application/octet-stream"
                : MimeTypeMap.TryGetValue(extension, out string? mimeType) ? mimeType : "application/octet-stream";
        }
    }
}
