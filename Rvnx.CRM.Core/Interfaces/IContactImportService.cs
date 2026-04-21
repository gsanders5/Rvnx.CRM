using Rvnx.CRM.Core.DTOs.Contact;

namespace Rvnx.CRM.Core.Interfaces;

public interface IContactImportService
{
    /// <summary>
    /// Parses a vCard (.vcf) stream and imports the contacts into the database.
    /// Handles duplicate detection and basic property mapping.
    /// </summary>
    /// <param name="vCardStream">The stream containing vCard data.</param>
    /// <returns>A <see cref="ContactImportResult"/> with the count of added and skipped contacts.</returns>
    Task<ContactImportResult> ImportFromVCardAsync(Stream vCardStream);
}
