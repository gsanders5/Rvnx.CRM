using Rvnx.CRM.Core.DTOs.Contact;

namespace Rvnx.CRM.Core.Interfaces;

public interface IContactReadService
{
    Task<List<ContactDto>> GetIndexDataAsync(bool showHidden);
    Task<ContactDetailDto?> GetContactDetailsAsync(Guid id);
    Task<ContactFormDto?> GetContactFormAsync(Guid id);
    Task<bool> ContactExistsAsync(Guid id);
}
