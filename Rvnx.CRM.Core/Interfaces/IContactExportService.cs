using Rvnx.CRM.Core.DTOs.Contact;

namespace Rvnx.CRM.Core.Interfaces;

public interface IContactExportService
{
    /// <summary>
    /// Generates a vCard (.vcf) file for a specific contact.
    /// Includes basic details, contact methods, and addresses.
    /// </summary>
    /// <param name="contactId">The ID of the contact to export.</param>
    /// <returns>A <see cref="ContactExportResult"/> containing the file content and metadata.</returns>
    Task<ContactExportResult> ExportToVCardAsync(Guid contactId);
}
