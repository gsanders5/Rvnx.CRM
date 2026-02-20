namespace Rvnx.CRM.Core.Interfaces;

public interface IContactImportService
{
    /// <summary>
    /// Imports contacts from a VCard stream.
    /// Handles parsing, duplicate detection, and persistence of contacts and related entities.
    /// </summary>
    /// <param name="vCardStream">The stream containing VCard data.</param>
    /// <returns>A tuple containing the count of added contacts and skipped (duplicate) contacts.</returns>
    Task<(int Added, int Skipped)> ImportContactsAsync(Stream vCardStream);
}
