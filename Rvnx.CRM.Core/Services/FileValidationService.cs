using Rvnx.CRM.Core.Interfaces;

namespace Rvnx.CRM.Core.Services
{
    public class FileValidationService : IFileValidationService
    {
        private sealed record FileTypeInfo(string MimeType, bool IsImage, byte[]? MagicBytes);

        private static readonly Dictionary<string, FileTypeInfo> FileTypeMap = new(StringComparer.OrdinalIgnoreCase)
        {
            { ".jpg", new FileTypeInfo("image/jpeg", true, new byte[] { 0xFF, 0xD8, 0xFF }) },
            { ".jpeg", new FileTypeInfo("image/jpeg", true, new byte[] { 0xFF, 0xD8, 0xFF }) },
            { ".png", new FileTypeInfo("image/png", true, new byte[] { 0x89, 0x50, 0x4E, 0x47 }) },
            { ".gif", new FileTypeInfo("image/gif", true, new byte[] { 0x47, 0x49, 0x46, 0x38 }) },
            { ".pdf", new FileTypeInfo("application/pdf", false, new byte[] { 0x25, 0x50, 0x44, 0x46 }) },
            { ".txt", new FileTypeInfo("text/plain", false, null) },
            { ".doc", new FileTypeInfo("application/msword", false, new byte[] { 0xD0, 0xCF, 0x11, 0xE0 }) },
            { ".docx", new FileTypeInfo("application/vnd.openxmlformats-officedocument.wordprocessingml.document", false, new byte[] { 0x50, 0x4B, 0x03, 0x04 }) },
            { ".xls", new FileTypeInfo("application/vnd.ms-excel", false, new byte[] { 0xD0, 0xCF, 0x11, 0xE0 }) },
            { ".xlsx", new FileTypeInfo("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", false, new byte[] { 0x50, 0x4B, 0x03, 0x04 }) },
            { ".vcf", new FileTypeInfo("text/vcard", false, null) }
        };

        private const long MaxFileSize = 30 * 1024 * 1024; // 30 MB

        public bool IsAllowedExtension(string extension)
        {
            return !string.IsNullOrEmpty(extension) && FileTypeMap.ContainsKey(extension);
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

            return FileTypeMap.TryGetValue(extension, out var info) && info.IsImage;
        }

        public bool IsValidImageSignature(byte[] fileBytes, string? extension)
        {
            if (fileBytes == null || fileBytes.Length < 4 || string.IsNullOrEmpty(extension))
            {
                return false;
            }

            if (!FileTypeMap.TryGetValue(extension, out var info) || !info.IsImage)
            {
                return false;
            }

            return CheckSignature(fileBytes, info.MagicBytes);
        }

        public bool IsValidFileSignature(byte[] fileBytes, string extension)
        {
            if (fileBytes == null || fileBytes.Length < 4 || string.IsNullOrEmpty(extension))
            {
                return false;
            }

            if (!FileTypeMap.TryGetValue(extension, out var info))
            {
                return false;
            }

            return CheckSignature(fileBytes, info.MagicBytes);
        }

        public string GetMimeType(string extension)
        {
            if (string.IsNullOrEmpty(extension))
            {
                return "application/octet-stream";
            }

            return FileTypeMap.TryGetValue(extension, out var info) ? info.MimeType : "application/octet-stream";
        }

        private static bool CheckSignature(byte[] fileBytes, byte[]? magicBytes)
        {
            if (magicBytes == null || magicBytes.Length == 0)
            {
                return true;
            }

            if (fileBytes.Length < magicBytes.Length)
            {
                return false;
            }

            for (int i = 0; i < magicBytes.Length; i++)
            {
                if (fileBytes[i] != magicBytes[i])
                {
                    return false;
                }
            }

            return true;
        }
    }
}
