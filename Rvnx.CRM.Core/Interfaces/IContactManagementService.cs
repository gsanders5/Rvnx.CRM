using Rvnx.CRM.Core.DTOs.Contact;

namespace Rvnx.CRM.Core.Interfaces;

public interface IContactManagementService
{
    Task DeleteContactAsync(Guid contactId);
    Task<ContactOperationResult> CreateContactAsync(ContactFormDto contactDto);
    Task<ContactOperationResult> UpdateContactAsync(Guid id, ContactFormDto contactDto, Stream? imageStream, string? fileName, string? contentType);
}
