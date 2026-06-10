using Rvnx.CRM.Core.DTOs.Contact;

namespace Rvnx.CRM.Core.Interfaces;

public interface ICsvImportService
{
    /// <summary>
    /// Parses a CSV stream in the format produced by <see cref="ICsvExportService"/> and imports
    /// the contacts into the database. Handles duplicate detection and basic property mapping.
    /// </summary>
    /// <param name="csvStream">The stream containing CSV data.</param>
    /// <returns>A <see cref="ContactImportResult"/> with the count of added and skipped contacts.</returns>
    Task<ContactImportResult> ImportFromCsvAsync(Stream csvStream);
}
