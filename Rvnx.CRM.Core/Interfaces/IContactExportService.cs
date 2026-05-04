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

    /// <summary>
    /// Generates a zip archive containing one vCard (.vcf) file per non-partial contact.
    /// Hidden contacts are included; partial contacts are excluded.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="ContactExportResult"/> containing the zip file content and metadata.</returns>
    Task<ContactExportResult> ExportAllToVCardZipAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a zip archive containing one vCard (.vcf) file per contact in the supplied set.
    /// Partial contacts and IDs that don't belong to the current tenant are filtered out.
    /// </summary>
    /// <param name="contactIds">The contact IDs to export.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="ContactExportResult"/> containing the zip file content and metadata.</returns>
    Task<ContactExportResult> ExportSelectedToVCardZipAsync(
        IReadOnlyCollection<Guid> contactIds,
        CancellationToken cancellationToken = default);
}
