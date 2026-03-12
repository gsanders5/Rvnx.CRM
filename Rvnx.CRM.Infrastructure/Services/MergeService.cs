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

            // Move Attachments
            var attachments = await _repository.ListAsync<Attachment>(a => a.ContactId == secondaryId);
            foreach (var att in attachments)
            {
                att.ContactId = primaryId;
            }

            var primaryProfilePhotos = await _repository.ListAsync<Attachment>(a => a.ContactId == primaryId && a.AttachmentType == "ProfileImage");
            var secondaryProfilePhotos = await _repository.ListAsync<Attachment>(a => a.ContactId == secondaryId && a.AttachmentType == "ProfileImage");

            if (primaryProfilePhotos.Count > 0)
            {
                // Downgrade secondary's profile photos to general attachments so they don't conflict
                foreach (var photo in secondaryProfilePhotos)
                {
                    photo.AttachmentType = "General";
                    photo.ContactId = primaryId;
                }
            }
            else
            {
                // Keep secondary's as ProfileImage and just move them
                foreach (var photo in secondaryProfilePhotos)
                {
                    photo.ContactId = primaryId;
                }
            }

            // Move Notes
            var notes = await _repository.ListAsync<Note>(n => n.ContactId == secondaryId);
            foreach (var note in notes)
            {
                note.ContactId = primaryId;
            }

            // Move Contact Methods
            var primaryMethods = await _repository.ListAsync<ContactMethod>(m => m.ContactId == primaryId);
            var secondaryMethods = await _repository.ListAsync<ContactMethod>(m => m.ContactId == secondaryId);

            var existingMethods = primaryMethods.Select(m => (m.Type, m.Value.ToLowerInvariant())).ToHashSet();
            foreach (var method in secondaryMethods)
            {
                if (!existingMethods.Contains((method.Type, method.Value.ToLowerInvariant())))
                {
                    method.ContactId = primaryId;
                    existingMethods.Add((method.Type, method.Value.ToLowerInvariant()));
                }
                else
                {
                    await _repository.DeleteAsync<ContactMethod>(method.Id);
                }
            }

            // Move Significant Dates
            var primaryDates = await _repository.ListAsync<SignificantDate>(d => d.ContactId == primaryId);
            var secondaryDates = await _repository.ListAsync<SignificantDate>(d => d.ContactId == secondaryId);

            var existingDates = primaryDates.Select(d => (d.Title?.ToLowerInvariant(), d.EventDate)).ToHashSet();
            foreach (var date in secondaryDates)
            {
                if (!existingDates.Contains((date.Title?.ToLowerInvariant(), date.EventDate)))
                {
                    date.ContactId = primaryId;
                    existingDates.Add((date.Title?.ToLowerInvariant(), date.EventDate));
                }
                else
                {
                    await _repository.DeleteAsync<SignificantDate>(date.Id);
                }
            }

            // Move Facts
            var facts = await _repository.ListAsync<Fact>(f => f.ContactId == secondaryId);
            foreach (var fact in facts)
            {
                fact.ContactId = primaryId;
            }

            // Move Relationships
            var primaryRels = await _repository.ListAsync<Relationship>(r => r.EntityId == primaryId);
            var secondaryRels = await _repository.ListAsync<Relationship>(r => r.EntityId == secondaryId);

            var existingRels = primaryRels.Select(r => (r.RelatedEntityId, r.RelationshipTypeId)).ToHashSet();
            foreach (var rel in secondaryRels)
            {
                if (!existingRels.Contains((rel.RelatedEntityId, rel.RelationshipTypeId)))
                {
                    rel.EntityId = primaryId;
                    existingRels.Add((rel.RelatedEntityId, rel.RelationshipTypeId));
                }
                else
                {
                    await _repository.DeleteAsync<Relationship>(rel.Id);
                }
            }

            // Also handle relationships where secondary is the RelatedEntityId
            var relatedToSecondary = await _repository.ListAsync<Relationship>(r => r.RelatedEntityId == secondaryId);
            foreach (var rel in relatedToSecondary)
            {
                if (rel.EntityId == primaryId)
                {
                    // Primary is already related to Secondary, delete this duplicate connection
                    await _repository.DeleteAsync<Relationship>(rel.Id);
                }
                else
                {
                    // Check if rel.EntityId is already related to Primary with the same RelationshipTypeId
                    var checkPrimaryRel = primaryId; // Need local variable for expression
                    var checkEntityRel = rel.EntityId;
                    var checkTypeId = rel.RelationshipTypeId;
                    var exists = await _repository.ListAsync<Relationship>(r => r.EntityId == checkEntityRel && r.RelatedEntityId == checkPrimaryRel && r.RelationshipTypeId == checkTypeId);
                    if (exists.Count == 0)
                    {
                        rel.RelatedEntityId = primaryId;
                    }
                    else
                    {
                        await _repository.DeleteAsync<Relationship>(rel.Id);
                    }
                }
            }

            // Move Pets
            var primaryPets = await _repository.ListAsync<Pet>(p => p.ContactId == primaryId);
            var secondaryPets = await _repository.ListAsync<Pet>(p => p.ContactId == secondaryId);

            var existingPets = primaryPets.Select(p => p.Name.ToLowerInvariant()).ToHashSet();
            foreach (var pet in secondaryPets)
            {
                if (!existingPets.Contains(pet.Name.ToLowerInvariant()))
                {
                    pet.ContactId = primaryId;
                    existingPets.Add(pet.Name.ToLowerInvariant());
                }
                else
                {
                    await _repository.DeleteAsync<Pet>(pet.Id);
                }
            }

            await _repository.UpdateAsync(primary);

            // Delete secondary contact
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
        if (!string.IsNullOrWhiteSpace(primary))
            return primary;
        if (!string.IsNullOrWhiteSpace(secondary))
            return secondary;
        return null;
    }
}
