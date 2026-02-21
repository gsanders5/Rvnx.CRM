using Rvnx.CRM.Core.DTOs.Contact;

namespace Rvnx.CRM.Core.Interfaces;

public interface IContactExportService
{
    Task<ContactExportResult> ExportToVCardAsync(Guid contactId);
}
