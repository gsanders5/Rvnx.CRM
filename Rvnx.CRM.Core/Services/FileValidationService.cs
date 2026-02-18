using Rvnx.CRM.Core.Interfaces;

namespace Rvnx.CRM.Core.Services
{
    public class FileValidationService : IFileValidationService
    {
        public bool IsImageExtension(string extension)
        {
            if (string.IsNullOrEmpty(extension)) return false;
            extension = extension.ToLowerInvariant();
            return extension is ".jpg" or ".jpeg" or ".png" or ".gif";
        }

        public bool IsValidImageSignature(byte[] fileBytes, string extension)
        {
            if (fileBytes == null || fileBytes.Length < 4 || string.IsNullOrEmpty(extension)) return false;

            extension = extension.ToLowerInvariant();

            return extension switch
            {
                ".jpg" or ".jpeg" => fileBytes[0] == 0xFF && fileBytes[1] == 0xD8 && fileBytes[2] == 0xFF,
                ".png" => fileBytes[0] == 0x89 && fileBytes[1] == 0x50 && fileBytes[2] == 0x4E && fileBytes[3] == 0x47,
                ".gif" => fileBytes[0] == 0x47 && fileBytes[1] == 0x49 && fileBytes[2] == 0x46 && fileBytes[3] == 0x38,
                _ => false
            };
        }
    }
}
