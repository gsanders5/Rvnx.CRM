namespace Rvnx.CRM.Core.Interfaces
{
    public interface IFileValidationService
    {
        bool IsValidImageSignature(byte[] fileBytes, string extension);
        bool IsImageExtension(string extension);
    }
}
