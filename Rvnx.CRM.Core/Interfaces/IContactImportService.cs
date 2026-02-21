using Rvnx.CRM.Core.DTOs.Contact;

namespace Rvnx.CRM.Core.Interfaces;

public interface IContactImportService
{
    Task<ContactImportResult> ImportFromVCardAsync(Stream vCardStream);
}
