namespace Rvnx.CRM.Core.Interfaces
{
    public interface IFileValidationService
    {
        bool IsValidImageSignature(byte[] fileBytes, string extension);
        bool IsImageExtension(string extension);
        bool IsValidFileSignature(byte[] fileBytes, string extension);
        bool IsAllowedExtension(string extension);
        bool IsAllowedFileSize(long length);
        string GetMimeType(string extension);
    }
}
