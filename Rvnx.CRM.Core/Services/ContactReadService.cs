using Rvnx.CRM.Core.Constants;
using Rvnx.CRM.Core.DTOs.Contact;
using Rvnx.CRM.Core.Enumerations;
using Rvnx.CRM.Core.Extensions;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models.Base;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Core.Models.Dates;

namespace Rvnx.CRM.Core.Services;

public class ContactReadService(IRepository repository) : IContactReadService
{
    private readonly IRepository _repository = repository;

    public async Task<List<ContactDto>> GetIndexDataAsync(bool showHidden)
    {
        List<Contact> contacts = await _repository.ListAsNoTrackingAsync<Contact>(x => x.IsHidden == showHidden);

        List<ContactDto> contactDtos = [.. contacts.Select(c => c.ToDto())];
        List<Guid> contactIds = [.. contacts.Select(c => c.Id)];

        List<Attachment> profileAttachments = contactIds.Count < 2000
            ? await _repository.ListAsNoTrackingAsync<Attachment>(a => a.EntityType == EntityTypes.Person
                && a.AttachmentType == AttachmentTypes.ProfileImage
                && contactIds.Contains(a.EntityId))
            : await _repository.ListAsNoTrackingAsync<Attachment>(a => a.EntityType == EntityTypes.Person
                && a.AttachmentType == AttachmentTypes.ProfileImage);

        // Optimization: If contact list is large, avoid sending large list of IDs to SQL (which can hit parameter limits).
        // Instead, fetch all profile images for Person entities (scoped by user via global filter) and filter in memory.

        if (profileAttachments != null && profileAttachments.Any())
        {
            Dictionary<Guid, Attachment> attachmentMap = profileAttachments
                .Where(a => a != null)
                .GroupBy(a => a.EntityId) // Handle potential duplicates gracefully
                .ToDictionary(g => g.Key, g => g.First());

            foreach (ContactDto? dto in contactDtos)
            {
                if (dto != null && attachmentMap.TryGetValue(dto.Id, out Attachment? attachment))
                {
                    dto.ProfileImageId = attachment.Id;
                }
            }
        }

        return contactDtos;
    }

    public async Task<ContactDetailDto?> GetContactDetailsAsync(Guid id)
    {
        // Optimization: Use ListAsNoTrackingAsync to avoid change tracking overhead for read-only operation
        List<Contact> contacts = await _repository.ListAsNoTrackingAsync<Contact>(c => c.Id == id, default, "Employers");
        Contact? contact = contacts.FirstOrDefault();
        if (contact == null) return null;

        contact.Notes = await GetRelatedEntitiesAsync<Note>(id);
        contact.Reminders = await GetRelatedEntitiesAsync<Reminder>(id);
        contact.SignificantDates = await GetRelatedEntitiesAsync<SignificantDate>(id);

        // Relationships Optimization: Fetch both outgoing and incoming relationships in one query
        List<Relationship> allRelationships = await _repository.ListAsNoTrackingAsync<Relationship>(r =>
            (r.EntityId == id && r.EntityType == EntityTypes.Person) ||
            (r.RelatedEntityId == id && r.EntityType == EntityTypes.Person));

        List<Relationship> relationships = allRelationships.Where(r => r.EntityId == id).ToList();
        List<Relationship> relatedTo = allRelationships.Where(r => r.RelatedEntityId == id).ToList();

        // Fetch all related contacts in one go
        List<Guid> relatedIds = relationships.Select(r => r.RelatedEntityId)
            .Concat(relatedTo.Select(r => r.EntityId))
            .Distinct()
            .ToList();

        List<Contact> relatedContacts = new();
        if (relatedIds.Any())
        {
            relatedContacts = await _repository.ListAsNoTrackingAsync<Contact>(c => relatedIds.Contains(c.Id));
        }

        // Manually populate navigation properties for display
        foreach (Relationship rel in relationships)
        {
            rel.RelatedPerson = relatedContacts.FirstOrDefault(c => c.Id == rel.RelatedEntityId);
        }
        contact.Relationships = relationships;

        foreach (Relationship rel in relatedTo)
        {
            rel.Person = relatedContacts.FirstOrDefault(c => c.Id == rel.EntityId);
        }
        contact.RelatedTo = relatedTo;

        // Pets
        List<Pet> pets = await GetRelatedEntitiesAsync<Pet>(id);

        // Contact Infos
        contact.ContactMethods = await GetRelatedEntitiesAsync<ContactMethod>(id);

        // Facts
        contact.Facts = await GetRelatedEntitiesAsync<Fact>(id);

        // Attachments Optimization: Fetch all attachments (profile & others) in one query
        List<Attachment> allAttachments = await _repository.ListAsNoTrackingAsync<Attachment>(a => a.EntityId == id && a.EntityType == EntityTypes.Person);

        contact.Attachments = allAttachments
            .Where(a => a.AttachmentType != AttachmentTypes.ProfileImage)
            .ToList();

        ContactDetailDto contactDto = contact.ToDetailDto();
        contactDto.Pets = pets.Select(p => p.ToDto()).ToList();

        Attachment? profileAttachment = allAttachments
            .FirstOrDefault(a => a.AttachmentType == AttachmentTypes.ProfileImage);

        if (profileAttachment != null)
        {
            contactDto.ProfileImageId = profileAttachment.Id;
        }

        return contactDto;
    }

    public async Task<ContactFormDto?> GetContactFormAsync(Guid id)
    {
        Contact? contact = await _repository.GetByIdAsync<Contact>(id);
        if (contact == null) return null;

        ContactFormDto dto = new()
        {
            Id = contact.Id,
            FirstName = contact.FirstName,
            LastName = contact.LastName ?? string.Empty,
            Nickname = contact.Nickname,
            JobTitle = contact.JobTitle,
            Company = contact.Company,
            IsHidden = contact.IsHidden
        };

        ContactMethod? email = await GetPrimaryContactMethodAsync(contact.Id, ContactMethodType.Email);
        dto.Email = email?.Value;

        ContactMethod? phone = await GetPrimaryContactMethodAsync(contact.Id, ContactMethodType.Phone);
        dto.Phone = phone?.Value;

        SignificantDate? bday = await GetBirthdayAsync(contact.Id);
        dto.Birthday = bday?.Date;

        return dto;
    }

    public async Task<bool> ContactExistsAsync(Guid id)
    {
        return await _repository.ExistsAsync<Contact>(id);
    }

    private async Task<List<T>> GetRelatedEntitiesAsync<T>(Guid contactId) where T : PolymorphicEntity
    {
        return await _repository.ListAsNoTrackingAsync<T>(e => e.EntityId == contactId && e.EntityType == EntityTypes.Person);
    }

    private async Task<ContactMethod?> GetPrimaryContactMethodAsync(Guid contactId, ContactMethodType type)
    {
        List<ContactMethod> methods = await _repository.ListAsNoTrackingAsync<ContactMethod>(c => c.EntityId == contactId && c.EntityType == EntityTypes.Person && c.Type == type);
        return methods.FirstOrDefault(e => e.Label == ContactMethodLabels.Primary) ?? methods.FirstOrDefault();
    }

    private async Task<SignificantDate?> GetBirthdayAsync(Guid contactId)
    {
        List<SignificantDate> bdays = await _repository.ListAsNoTrackingAsync<SignificantDate>(d => d.EntityId == contactId && d.EntityType == EntityTypes.Person && d.Title == SignificantDateTitles.Birthday);
        return bdays.FirstOrDefault();
    }
}
