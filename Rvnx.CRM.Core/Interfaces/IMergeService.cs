namespace Rvnx.CRM.Core.Interfaces;

public interface IMergeService
{
    /// <summary>
    /// Merges the secondary contact into the primary contact.
    /// The secondary contact is deleted upon successful merge.
    /// </summary>
    /// <param name="primaryId">The ID of the contact to keep.</param>
    /// <param name="secondaryId">The ID of the contact to merge into the primary contact and then delete.</param>
    /// <returns>A Task representing the asynchronous operation.</returns>
    Task MergeContactsAsync(Guid primaryId, Guid secondaryId);
}