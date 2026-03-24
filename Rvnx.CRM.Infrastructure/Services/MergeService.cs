using Microsoft.EntityFrameworkCore;
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

        Contact? primary = await _repository.QueryUnfiltered<Contact>().FirstOrDefaultAsync(c => c.Id == primaryId);
        Contact? secondary = await _repository.QueryUnfiltered<Contact>().FirstOrDefaultAsync(c => c.Id == secondaryId);

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
            // 1. Scalar fields
            primary.FirstName = MergeScalar(primary.FirstName, secondary.FirstName) ?? string.Empty;
            primary.LastName = MergeScalar(primary.LastName, secondary.LastName);
            primary.MaidenName = MergeScalar(primary.MaidenName, secondary.MaidenName);
            primary.Nickname = MergeScalar(primary.Nickname, secondary.Nickname);
            primary.JobTitle = MergeScalar(primary.JobTitle, secondary.JobTitle);
            primary.Company = MergeScalar(primary.Company, secondary.Company);
            primary.Pronouns = MergeScalar(primary.Pronouns, secondary.Pronouns);
            primary.Gender = MergeScalar(primary.Gender, secondary.Gender);
            primary.Religion = MergeScalar(primary.Religion, secondary.Religion);

            List<Attachment> attachments = await _repository.ListAsync<Attachment>(a => a.ContactId == secondaryId);
            foreach (Attachment att in attachments)
            {
                att.ContactId = primaryId;
            }

            List<Attachment> primaryProfilePhotos = await _repository.ListAsync<Attachment>(a => a.ContactId == primaryId && a.AttachmentType == "ProfileImage");
            List<Attachment> secondaryProfilePhotos = await _repository.ListAsync<Attachment>(a => a.ContactId == secondaryId && a.AttachmentType == "ProfileImage");

            if (primaryProfilePhotos.Count > 0)
            {
                // Downgrade secondary's profile photos to general attachments so they don't conflict
                foreach (Attachment photo in secondaryProfilePhotos)
                {
                    photo.AttachmentType = "General";
                    photo.ContactId = primaryId;
                }
            }
            else
            {
                // Keep secondary's as ProfileImage and just move them
                foreach (Attachment photo in secondaryProfilePhotos)
                {
                    photo.ContactId = primaryId;
                }
            }

            // Move Notes
            List<Note> notes = await _repository.ListAsync<Note>(n => n.ContactId == secondaryId);
            foreach (Note note in notes)
            {
                note.ContactId = primaryId;
            }

            List<ContactMethod> primaryMethods = await _repository.ListAsync<ContactMethod>(m => m.ContactId == primaryId);
            List<ContactMethod> secondaryMethods = await _repository.ListAsync<ContactMethod>(m => m.ContactId == secondaryId);

            HashSet<(Core.Enumerations.ContactMethodType Type, string)> existingMethods = new(primaryMethods.Count);
            foreach (var m in primaryMethods)
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
            foreach (var d in primaryDates)
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

            List<Relationship> primaryRels = await _repository.ListAsync<Relationship>(r => r.EntityId == primaryId);
            List<Relationship> secondaryRels = await _repository.ListAsync<Relationship>(r => r.EntityId == secondaryId);

            HashSet<(Guid RelatedEntityId, Guid RelationshipTypeId)> existingRels = new(primaryRels.Count);
            foreach (var r in primaryRels)
            {
                existingRels.Add((r.RelatedEntityId, r.RelationshipTypeId));
            }

            List<Relationship> relsToDelete = [];
            foreach (Relationship rel in secondaryRels)
            {
                if (existingRels.Add((rel.RelatedEntityId, rel.RelationshipTypeId)))
                {
                    rel.EntityId = primaryId;
                }
                else
                {
                    relsToDelete.Add(rel);
                }
            }

            // Also handle relationships where secondary is the RelatedEntityId
            List<Relationship> relatedToSecondary = await _repository.ListAsync<Relationship>(r => r.RelatedEntityId == secondaryId);

            // Pre-fetch all relationships pointing to primary to avoid N+1 queries
            List<Relationship> relatedToPrimary = await _repository.ListAsync<Relationship>(r => r.RelatedEntityId == primaryId);
            HashSet<(Guid EntityId, Guid RelationshipTypeId)> existingInverseRels = new(relatedToPrimary.Count);
            foreach (var r in relatedToPrimary)
            {
                existingInverseRels.Add((r.EntityId, r.RelationshipTypeId));
            }

            foreach (Relationship rel in relatedToSecondary)
            {
                if (rel.EntityId == primaryId)
                {
                    // Primary is already related to Secondary, delete this duplicate connection
                    relsToDelete.Add(rel);
                }
                else
                {
                    // Check if rel.EntityId is already related to Primary with the same RelationshipTypeId
                    if (existingInverseRels.Add((rel.EntityId, rel.RelationshipTypeId)))
                    {
                        rel.RelatedEntityId = primaryId;
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

            List<Pet> primaryPets = await _repository.ListAsync<Pet>(p => p.ContactId == primaryId);
            List<Pet> secondaryPets = await _repository.ListAsync<Pet>(p => p.ContactId == secondaryId);

            HashSet<string> existingPets = new(primaryPets.Count);
            foreach (var p in primaryPets)
            {
                existingPets.Add(p.Name.ToLowerInvariant());
            }

            List<Pet> petsToDelete = [];
            foreach (Pet pet in secondaryPets)
            {
                if (existingPets.Add(pet.Name.ToLowerInvariant()))
                {
                    pet.ContactId = primaryId;
                }
                else
                {
                    petsToDelete.Add(pet);
                }
            }
            if (petsToDelete.Count > 0)
            {
                await _repository.DeleteRangeAsync(petsToDelete);
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