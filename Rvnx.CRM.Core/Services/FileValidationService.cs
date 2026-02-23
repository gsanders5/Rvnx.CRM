using Rvnx.CRM.Core.Interfaces;

namespace Rvnx.CRM.Core.Services
{
    public class FileValidationService : IFileValidationService
    {
        private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".gif", ".pdf", ".txt", ".doc", ".docx", ".xls", ".xlsx" };
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
            if (string.IsNullOrEmpty(extension))
            {
                return false;
            }

            extension = extension.ToLowerInvariant();
            return extension is ".jpg" or ".jpeg" or ".png" or ".gif";
        }

        public bool IsValidImageSignature(byte[] fileBytes, string? extension)
        {
            if (fileBytes == null || fileBytes.Length < 4 || string.IsNullOrEmpty(extension))
            {
                return false;
            }

            extension = extension.ToLowerInvariant();

            return extension switch
            {
                ".jpg" or ".jpeg" => fileBytes[0] == 0xFF && fileBytes[1] == 0xD8 && fileBytes[2] == 0xFF,
                ".png" => fileBytes[0] == 0x89 && fileBytes[1] == 0x50 && fileBytes[2] == 0x4E && fileBytes[3] == 0x47,
                ".gif" => fileBytes[0] == 0x47 && fileBytes[1] == 0x49 && fileBytes[2] == 0x46 && fileBytes[3] == 0x38,
                _ => false
            };
        }

        public bool IsValidFileSignature(byte[] fileBytes, string extension)
        {
            if (fileBytes == null || fileBytes.Length < 4 || string.IsNullOrEmpty(extension))
            {
                return false;
            }

            if (!IsAllowedExtension(extension))
            {
                return false;
            }

            extension = extension.ToLowerInvariant();

            return IsImageExtension(extension)
                ? IsValidImageSignature(fileBytes, extension)
                : extension switch
                {
                    ".pdf" => fileBytes[0] == 0x25 && fileBytes[1] == 0x50 && fileBytes[2] == 0x44 && fileBytes[3] == 0x46,
                    ".doc" or ".xls" => fileBytes[0] == 0xD0 && fileBytes[1] == 0xCF && fileBytes[2] == 0x11 && fileBytes[3] == 0xE0,
                    ".docx" or ".xlsx" => fileBytes[0] == 0x50 && fileBytes[1] == 0x4B && fileBytes[2] == 0x03 && fileBytes[3] == 0x04,
                    ".txt" => true,
                    _ => false
                };
        }
    }
}
