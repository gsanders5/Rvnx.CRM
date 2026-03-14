using Rvnx.CRM.Core.Models.Contact;

namespace Rvnx.CRM.Core.Interfaces;

public interface IVCardService
{
    /// <summary>
    /// Parses a VCF stream asynchronously and returns a list of Contact entities.
    /// The returned entities are not yet saved to the database.
    /// Related entities (ContactMethods, SignificantDates, Attachments) are populated in the navigation properties.
    /// </summary>
    /// <param name="fileStream">The stream containing VCF data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of Contact entities.</returns>
    Task<IEnumerable<Contact>> ParseVCardAsync(Stream fileStream, CancellationToken cancellationToken = default);

    /// <summary>
    /// Serializes a Contact entity to VCard 3.0 format.
    /// The Contact must have its navigation properties (ContactMethods, SignificantDates, Attachments) populated.
    /// </summary>
    /// <param name="contact">The contact to export.</param>
    /// <returns>The VCF data as a byte array.</returns>
    byte[] ExportVCard(Contact contact);
}