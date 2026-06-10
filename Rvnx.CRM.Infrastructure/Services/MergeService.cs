using Microsoft.EntityFrameworkCore;
using Rvnx.CRM.Core.Constants;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models.Base;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Core.Models.Dates;
using Rvnx.CRM.Infrastructure.Data;

namespace Rvnx.CRM.Infrastructure.Services;

public class MergeService(CRMDbContext context, IRepository repository) : IMergeService
{
    private readonly CRMDbContext _context = context;
    private readonly IRepository _repository = repository;

    public async Task MergeContactsAsync(Guid primaryId, Guid secondaryId)
    {
        if (primaryId == secondaryId)
        {
            throw new InvalidOperationException("Cannot merge a contact with itself.");
        }

        // Fetch through the group-filtered set (not QueryUnfiltered) so a caller can only merge
        // contacts that belong to their own group. A GUID from another group resolves to null
        // here and is rejected below, preventing a cross-group destructive merge.
        Contact? primary = await _context.Set<Contact>().FirstOrDefaultAsync(c => c.Id == primaryId);
        Contact? secondary = await _context.Set<Contact>().FirstOrDefaultAsync(c => c.Id == secondaryId);

        if (primary == null || secondary == null)
        {
            throw new InvalidOperationException("One or both contacts not found.");
        }

        bool isRelational = _context.Database.IsRelational();
        Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction? transaction = null;

        if (isRelational)
        {
            transaction = await _context.Database.BeginTransactionAsync();
        }

        try
        {
            primary.FirstName = MergeScalar(primary.FirstName, secondary.FirstName) ?? string.Empty;
            primary.LastName = MergeScalar(primary.LastName, secondary.LastName);
            primary.MaidenName = MergeScalar(primary.MaidenName, secondary.MaidenName);
            primary.Nickname = MergeScalar(primary.Nickname, secondary.Nickname);
            primary.JobTitle = MergeScalar(primary.JobTitle, secondary.JobTitle);
            primary.Company = MergeScalar(primary.Company, secondary.Company);
            primary.Pronouns = MergeScalar(primary.Pronouns, secondary.Pronouns);
            primary.Gender = MergeScalar(primary.Gender, secondary.Gender);
            primary.Religion = MergeScalar(primary.Religion, secondary.Religion);
            primary.HowWeMet = MergeScalar(primary.HowWeMet, secondary.HowWeMet);
            primary.FirstMetOn ??= secondary.FirstMetOn;

            // Carry secondary's introducer forward when primary lacks one. Skip if it would point at the
            // primary itself (self-reference) or at the secondary (about to be deleted).
            if (!primary.IntroducedByContactId.HasValue
                && secondary.IntroducedByContactId.HasValue
                && secondary.IntroducedByContactId.Value != primaryId
                && secondary.IntroducedByContactId.Value != secondaryId)
            {
                primary.IntroducedByContactId = secondary.IntroducedByContactId;
            }
            else if (primary.IntroducedByContactId == secondaryId)
            {
                // Primary was introduced by the contact being merged in; clear to avoid SetNull cascade race.
                primary.IntroducedByContactId = null;
            }

            // Redirect any contact that was introduced by the secondary to point at the primary instead;
            // otherwise the FK SetNull cascade would silently lose those references when secondary is deleted.
            List<Contact> introducedBySecondary = await _repository.ListAsync<Contact>(c => c.IntroducedByContactId == secondaryId);
            foreach (Contact dependent in introducedBySecondary)
            {
                if (dependent.Id == primaryId)
                {
                    // Primary was already updated above; skip.
                    continue;
                }
                dependent.IntroducedByContactId = primaryId;
            }

            // Deceased status / date of death: deceased is a one-way truth. If either record
            // is marked deceased, the merged primary must be too — otherwise merging a deceased
            // duplicate into a stale alive record silently loses the death information (and
            // re-enables reminders for someone who has died). For DateOfDeath, prefer the
            // primary's value if set, else take the secondary's so the date is preserved.
            primary.IsDeceased = primary.IsDeceased || secondary.IsDeceased;
            primary.DateOfDeath ??= secondary.DateOfDeath;

            List<Attachment> attachments = await _repository.ListAsync<Attachment>(a => a.ContactId == secondaryId);
            foreach (Attachment att in attachments)
            {
                att.ContactId = primaryId;
            }

            List<Attachment> primaryProfilePhotos = await _repository.ListAsync<Attachment>(a => a.ContactId == primaryId && a.AttachmentType == AttachmentTypes.ProfileImage);
            List<Attachment> secondaryProfilePhotos = await _repository.ListAsync<Attachment>(a => a.ContactId == secondaryId && a.AttachmentType == AttachmentTypes.ProfileImage);

            if (primaryProfilePhotos.Count > 0)
            {
                // Downgrade secondary's profile photos to general attachments so they don't conflict
                foreach (Attachment photo in secondaryProfilePhotos)
                {
                    photo.AttachmentType = AttachmentTypes.General;
                    photo.ContactId = primaryId;
                }
            }
            else
            {
                foreach (Attachment photo in secondaryProfilePhotos)
                {
                    photo.ContactId = primaryId;
                }
            }

            List<Note> notes = await _repository.ListAsync<Note>(n => n.ContactId == secondaryId);
            foreach (Note note in notes)
            {
                note.ContactId = primaryId;
            }

            List<ContactMethod> primaryMethods = await _repository.ListAsync<ContactMethod>(m => m.ContactId == primaryId);
            List<ContactMethod> secondaryMethods = await _repository.ListAsync<ContactMethod>(m => m.ContactId == secondaryId);

            HashSet<(Core.Enumerations.ContactMethodType Type, string)> existingMethods = new(primaryMethods.Count);
            foreach (ContactMethod m in primaryMethods)
            {
                existingMethods.Add((m.Type, m.Value.ToLowerInvariant()));
            }

            List<ContactMethod> methodsToDelete = [];
            foreach (ContactMethod method in secondaryMethods)
            {
                if (existingMethods.Add((method.Type, method.Value.ToLowerInvariant())))
                {
                    method.ContactId = primaryId;
                }
                else
                {
                    methodsToDelete.Add(method);
                }
            }
            if (methodsToDelete.Count > 0)
            {
                await _repository.DeleteRangeAsync(methodsToDelete);
            }

            List<SignificantDate> primaryDates = await _repository.ListAsync<SignificantDate>(d => d.ContactId == primaryId);
            List<SignificantDate> secondaryDates = await _repository.ListAsync<SignificantDate>(d => d.ContactId == secondaryId);

            HashSet<(string?, DateOnly EventDate)> existingDates = new(primaryDates.Count);
            foreach (SignificantDate d in primaryDates)
            {
                existingDates.Add((d.Title?.ToLowerInvariant(), d.EventDate));
            }

            List<SignificantDate> datesToDelete = [];
            foreach (SignificantDate date in secondaryDates)
            {
                if (existingDates.Add((date.Title?.ToLowerInvariant(), date.EventDate)))
                {
                    date.ContactId = primaryId;
                }
                else
                {
                    datesToDelete.Add(date);
                }
            }
            if (datesToDelete.Count > 0)
            {
                await _repository.DeleteRangeAsync(datesToDelete);
            }

            List<Fact> facts = await _repository.ListAsync<Fact>(f => f.ContactId == secondaryId);
            foreach (Fact fact in facts)
            {
                fact.ContactId = primaryId;
            }

            List<Relationship> primaryRels = await _repository.ListAsync<Relationship>(r => r.ContactId == primaryId);
            List<Relationship> secondaryRels = await _repository.ListAsync<Relationship>(r => r.ContactId == secondaryId);

            HashSet<(Guid RelatedContactId, Guid RelationshipTypeId)> existingRels = new(primaryRels.Count);
            foreach (Relationship r in primaryRels)
            {
                existingRels.Add((r.RelatedContactId, r.RelationshipTypeId));
            }

            List<Relationship> relsToDelete = [];
            foreach (Relationship rel in secondaryRels)
            {
                if (existingRels.Add((rel.RelatedContactId, rel.RelationshipTypeId)))
                {
                    rel.ContactId = primaryId;
                }
                else
                {
                    relsToDelete.Add(rel);
                }
            }

            List<Relationship> relatedToSecondary = await _repository.ListAsync<Relationship>(r => r.RelatedContactId == secondaryId);

            // Pre-fetch all relationships pointing to primary to avoid N+1 queries
            List<Relationship> relatedToPrimary = await _repository.ListAsync<Relationship>(r => r.RelatedContactId == primaryId);
            HashSet<(Guid ContactId, Guid RelationshipTypeId)> existingInverseRels = new(relatedToPrimary.Count);
            foreach (Relationship r in relatedToPrimary)
            {
                existingInverseRels.Add((r.ContactId, r.RelationshipTypeId));
            }

            foreach (Relationship rel in relatedToSecondary)
            {
                if (rel.ContactId == primaryId)
                {
                    // Primary is already related to Secondary, delete this duplicate connection
                    relsToDelete.Add(rel);
                }
                else
                {
                    if (existingInverseRels.Add((rel.ContactId, rel.RelationshipTypeId)))
                    {
                        rel.RelatedContactId = primaryId;
                    }
                    else
                    {
                        relsToDelete.Add(rel);
                    }
                }
            }
            if (relsToDelete.Count > 0)
            {
                await _repository.DeleteRangeAsync(relsToDelete);
            }

            List<PetContact> primaryPetContacts = await _repository.ListAsync<PetContact>(
                pc => pc.ContactId == primaryId, default, nameof(PetContact.Pet));
            List<PetContact> secondaryPetContacts = await _repository.ListAsync<PetContact>(
                pc => pc.ContactId == secondaryId, default, nameof(PetContact.Pet));

            HashSet<string> existingPetNames = new(primaryPetContacts.Count);
            HashSet<Guid> primaryPetIds = new(primaryPetContacts.Count);
            foreach (PetContact pc in primaryPetContacts)
            {
                existingPetNames.Add(pc.Pet.Name.ToLowerInvariant());
                primaryPetIds.Add(pc.PetId);
            }

            List<PetContact> petContactsToDelete = [];
            List<Pet> orphanPetsToDelete = [];

            // Optimization: Collect new entities in a list to batch insert via AddRangeAsync outside the loop,
            // preventing N+1 database roundtrips.
            List<PetContact> petContactsToAdd = [];
            foreach (PetContact pc in secondaryPetContacts)
            {
                if (existingPetNames.Add(pc.Pet.Name.ToLowerInvariant()))
                {
                    if (!primaryPetIds.Contains(pc.PetId))
                    {
                        petContactsToAdd.Add(new PetContact
                        {
                            PetId = pc.PetId,
                            ContactId = primaryId
                        });
                    }
                }
                else
                {
                    if (!primaryPetIds.Contains(pc.PetId))
                    {
                        orphanPetsToDelete.Add(pc.Pet);
                    }
                }
                petContactsToDelete.Add(pc);
            }

            if (petContactsToAdd.Count > 0)
            {
                await _repository.AddRangeAsync(petContactsToAdd);
            }

            if (petContactsToDelete.Count > 0)
            {
                await _repository.DeleteRangeAsync(petContactsToDelete);
            }
            if (orphanPetsToDelete.Count > 0)
            {
                await _repository.DeleteRangeAsync(orphanPetsToDelete);
            }

            await _repository.UpdateAsync(primary);

            await _repository.DeleteAsync<Contact>(secondaryId);

            await _context.SaveChangesAsync();
            if (transaction != null)
            {
                await transaction.CommitAsync();
            }
        }
        catch
        {
            if (transaction != null)
            {
                await transaction.RollbackAsync();
            }
            throw;
        }
        finally
        {
            if (transaction != null)
            {
                await transaction.DisposeAsync();
            }
        }
    }

    private static string? MergeScalar(string? primary, string? secondary)
    {
        return !string.IsNullOrWhiteSpace(primary) ? primary : !string.IsNullOrWhiteSpace(secondary) ? secondary : null;
    }
}
