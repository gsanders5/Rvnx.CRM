using Rvnx.CRM.Core.DTOs.Contact;

namespace Rvnx.CRM.Core.Interfaces;

public interface ICsvExportService
{
    Task<ContactExportResult> ExportContactsAsync();
}
