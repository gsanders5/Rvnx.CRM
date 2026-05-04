using Rvnx.CRM.Core.DTOs.Contact;

namespace Rvnx.CRM.Core.Interfaces;

public interface ICsvExportService
{
    /// <summary>
    /// Exports contacts as a CSV file. When <paramref name="ids"/> is null or empty,
    /// all non-partial contacts are exported. Otherwise, only the contacts whose IDs
    /// are in the collection are exported.
    /// </summary>
    /// <param name="ids">Optional set of contact IDs to restrict the export to.</param>
    Task<ContactExportResult> ExportContactsAsync(IReadOnlyCollection<Guid>? ids = null);
}
