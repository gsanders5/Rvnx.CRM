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

            HashSet<(Core.Enumerations.ContactMethodType Type, string)> existingMethods = primaryMethods.Select(m => (m.Type, m.Value.ToLowerInvariant())).ToHashSet();
            List<ContactMethod> methodsToDelete = [];
            foreach (ContactMethod method in secondaryMethods)
            {
                if (!existingMethods.Contains((method.Type, method.Value.ToLowerInvariant())))
                {
                    method.ContactId = primaryId;
                    existingMethods.Add((method.Type, method.Value.ToLowerInvariant()));
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

            HashSet<(string?, DateOnly EventDate)> existingDates = primaryDates.Select(d => (d.Title?.ToLowerInvariant(), d.EventDate)).ToHashSet();
            List<SignificantDate> datesToDelete = [];
            foreach (SignificantDate date in secondaryDates)
            {
                if (!existingDates.Contains((date.Title?.ToLowerInvariant(), date.EventDate)))
                {
                    date.ContactId = primaryId;
                    existingDates.Add((date.Title?.ToLowerInvariant(), date.EventDate));
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

            HashSet<(Guid RelatedEntityId, Guid RelationshipTypeId)> existingRels = primaryRels.Select(r => (r.RelatedEntityId, r.RelationshipTypeId)).ToHashSet();
            List<Relationship> relsToDelete = [];
            foreach (Relationship rel in secondaryRels)
            {
                if (!existingRels.Contains((rel.RelatedEntityId, rel.RelationshipTypeId)))
                {
                    rel.EntityId = primaryId;
                    existingRels.Add((rel.RelatedEntityId, rel.RelationshipTypeId));
                }
                else
                {
                    relsToDelete.Add(rel);
                }
            }

            // Also handle relationships where secondary is the RelatedEntityId
            List<Relationship> relatedToSecondary = await _repository.ListAsync<Relationship>(r => r.RelatedEntityId == secondaryId);
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
                    Guid checkPrimaryRel = primaryId; // Need local variable for expression
                    Guid checkEntityRel = rel.EntityId;
                    Guid checkTypeId = rel.RelationshipTypeId;
                    List<Relationship> exists = await _repository.ListAsync<Relationship>(r => r.EntityId == checkEntityRel && r.RelatedEntityId == checkPrimaryRel && r.RelationshipTypeId == checkTypeId);
                    if (exists.Count == 0)
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

            HashSet<string> existingPets = primaryPets.Select(p => p.Name.ToLowerInvariant()).ToHashSet();
            List<Pet> petsToDelete = [];
            foreach (Pet pet in secondaryPets)
            {
                if (!existingPets.Contains(pet.Name.ToLowerInvariant()))
                {
                    pet.ContactId = primaryId;
                    existingPets.Add(pet.Name.ToLowerInvariant());
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