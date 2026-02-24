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
        List<Contact> contacts = await _repository.ListAsNoTrackingAsync<Contact>(x => x.IsHidden == showHidden && !x.IsPartial);

        List<ContactDto> contactDtos = [.. contacts.Select(c => c.ToDto())];
        List<Guid> contactIds = [.. contacts.Select(c => c.Id)];

        List<Attachment> profileAttachments = contactIds.Count > 0
            ? await _repository.ListByChunkedContainsAsync<Attachment, Guid>(
                contactIds,
                chunk => a => a.ContactId != null && a.AttachmentType == AttachmentTypes.ProfileImage && chunk.Contains(a.ContactId.Value))
            : [];

        if (profileAttachments.Count > 0)
        {
            Dictionary<Guid, Attachment> attachmentMap = profileAttachments
                .Where(a => a != null && a.ContactId.HasValue)
                .GroupBy(a => a.ContactId!.Value) // Handle potential duplicates gracefully
                .ToDictionary(g => g.Key, g => g.First());

            foreach (ContactDto? dto in contactDtos)
            {
                if (dto != null && attachmentMap.TryGetValue(dto.Id, out Attachment? attachment))
                {
                    dto.ProfileImageId = attachment.Id;
                }
            }
        }

        List<ContactLabel> allContactLabels = contactIds.Count > 0
            ? await _repository.ListByChunkedContainsAsync<ContactLabel, Guid>(
                contactIds,
                chunk => cl => chunk.Contains(cl.ContactId),
                asNoTracking: true,
                cancellationToken: default,
                nameof(ContactLabel.Label))
            : [];

        Dictionary<Guid, List<LabelDto>> labelsByContact = allContactLabels
            .GroupBy(cl => cl.ContactId)
            .ToDictionary(g => g.Key, g => g.Select(cl => new LabelDto { Id = cl.Label.Id, Name = cl.Label.Name, Color = cl.Label.Color }).OrderBy(l => l.Name).ToList());

        foreach (ContactDto? dto in contactDtos)
        {
            if (dto != null && labelsByContact.TryGetValue(dto.Id, out List<LabelDto>? labels))
            {
                dto.Labels = labels;
            }
        }

        return contactDtos;
    }

    public async Task<ContactDetailDto?> GetContactDetailsAsync(Guid id)
    {
        // Optimization: Use ListAsNoTrackingAsync to avoid change tracking overhead for read-only operation
        List<Contact> contacts = await _repository.ListAsNoTrackingAsync<Contact>(c => c.Id == id && !c.IsPartial, default,
            nameof(Contact.Employers),
            nameof(Contact.Pets),
            nameof(Contact.Notes),
            nameof(Contact.Reminders),
            nameof(Contact.SignificantDates),
            nameof(Contact.ContactMethods),
            nameof(Contact.Facts),
            nameof(Contact.Addresses),
            nameof(Contact.Attachments),
            nameof(Contact.ContactLabels) + "." + nameof(ContactLabel.Label));

        Contact? contact = contacts.FirstOrDefault();
        if (contact == null)
        {
            return null;
        }

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

        List<Contact> relatedContacts = [];
        if (relatedIds.Count > 0)
        {
            relatedContacts = await _repository.ListByChunkedContainsAsync<Contact, Guid>(
                relatedIds,
                chunk => c => chunk.Contains(c.Id),
                asNoTracking: true);
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

        ContactDetailDto contactDto = contact.ToDetailDto();
        contactDto.Pets = contact.Pets.Select(p => p.ToDto()).ToList();

        Attachment? profileAttachment = contact.Attachments
            .FirstOrDefault(a => a.AttachmentType == AttachmentTypes.ProfileImage);

        if (profileAttachment != null)
        {
            contactDto.ProfileImageId = profileAttachment.Id;
        }

        contactDto.Attachments = contactDto.Attachments
            .Where(a => a.AttachmentType != AttachmentTypes.ProfileImage)
            .ToList();

        contactDto.Labels = contact.ContactLabels.Select(cl => cl.Label).OrderBy(l => l.Name).Select(l => new LabelDto { Id = l.Id, Name = l.Name, Color = l.Color }).ToList();

        return contactDto;
    }

    public async Task<ContactFormDto?> GetContactFormAsync(Guid id)
    {
        List<Contact> contacts = await _repository.ListAsNoTrackingAsync<Contact>(
             c => c.Id == id && !c.IsPartial,
             default,
             nameof(Contact.ContactMethods),
             nameof(Contact.SignificantDates),
             nameof(Contact.ContactLabels));

        Contact? contact = contacts.FirstOrDefault();

        if (contact == null)
        {
            return null;
        }

        ContactFormDto dto = new()
        {
            Id = contact.Id,
            FirstName = contact.FirstName,
            LastName = contact.LastName ?? string.Empty,
            Nickname = contact.Nickname,
            JobTitle = contact.JobTitle,
            Company = contact.Company,
            IsHidden = contact.IsHidden,
            Pronouns = contact.Pronouns,
            Gender = contact.Gender,
            Religion = contact.Religion
        };

        ContactMethod? email = contact.ContactMethods
            .Where(c => c.Type == ContactMethodType.Email)
            .OrderByDescending(c => c.Label == ContactMethodLabels.Primary)
            .FirstOrDefault();
        dto.Email = email?.Value;

        ContactMethod? phone = contact.ContactMethods
            .Where(c => c.Type == ContactMethodType.Phone)
            .OrderByDescending(c => c.Label == ContactMethodLabels.Primary)
            .FirstOrDefault();
        dto.Phone = phone?.Value;

        SignificantDate? bday = contact.SignificantDates
            .FirstOrDefault(d => d.Title == SignificantDateTitles.Birthday);
        dto.Birthday = bday?.Date;

        List<Label> allLabels = await _repository.ListAsNoTrackingAsync<Label>(l => true) ?? [];

        dto.AllLabels = allLabels.OrderBy(l => l.Name).Select(l => new LabelDto { Id = l.Id, Name = l.Name, Color = l.Color }).ToList();
        dto.AssignedLabelIds = contact.ContactLabels.Select(cl => cl.LabelId).ToList();

        return dto;
    }

    public async Task<bool> ContactExistsAsync(Guid id)
    {
        Contact? c = await _repository.GetByIdAsync<Contact>(id);
        return c != null && !c.IsPartial;
    }
}
