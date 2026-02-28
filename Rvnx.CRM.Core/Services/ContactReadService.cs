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
        // Optimization: Project directly to DTO to avoid fetching all columns and instantiating Contact entities
        List<ContactDto> contactDtos = await _repository.ListProjectedAsync<Contact, ContactDto>(
            x => x.IsHidden == showHidden && !x.IsPartial,
            c => new ContactDto
            {
                Id = c.Id,
                FirstName = c.FirstName,
                LastName = c.LastName ?? string.Empty,
                // Optimization: Compute FullName in database projection to avoid extra memory loop
                FullName = (c.FirstName + " " + (c.LastName ?? "")).Trim(),
                Company = c.Company,
                JobTitle = c.JobTitle,
                IsHidden = c.IsHidden,
                CreatedDate = c.CreatedDate,
                LastChangedDate = c.LastChangedDate,
                CreatedBy = c.CreatedBy,
                LastChangedBy = c.LastChangedBy,
                UserId = c.UserId != null ? c.UserId.ToString() : null,
                Pronouns = c.Pronouns,
                Gender = c.Gender,
                Religion = c.Religion,
                IsPartial = c.IsPartial
            });

        List<Guid> contactIds = [.. contactDtos.Select(c => c.Id)];

        // Optimization: Project only necessary fields (ContactId, AttachmentId) instead of fetching full Attachment entities
        List<(Guid ContactId, Guid AttachmentId)> profileAttachments = contactIds.Count > 0
            ? await _repository.ListProjectedByChunkedContainsAsync<Attachment, (Guid, Guid), Guid>(
                contactIds,
                chunk => a => a.ContactId != null && a.AttachmentType == AttachmentTypes.ProfileImage && chunk.Contains(a.ContactId.Value),
                a => new ValueTuple<Guid, Guid>(a.ContactId!.Value, a.Id))
            : [];

        if (profileAttachments.Count > 0)
        {
            Dictionary<Guid, Guid> attachmentMap = profileAttachments
                .GroupBy(a => a.ContactId) // Handle potential duplicates gracefully
                .ToDictionary(g => g.Key, g => g.First().AttachmentId);

            foreach (ContactDto? dto in contactDtos)
            {
                if (dto != null && attachmentMap.TryGetValue(dto.Id, out Guid attachmentId))
                {
                    dto.ProfileImageId = attachmentId;
                }
            }
        }

        // Optimization: Project only necessary fields (ContactId, Label Info) instead of fetching ContactLabel + joined Label entities
        List<(Guid ContactId, Guid LabelId, string Name, string? Color)> allContactLabels = contactIds.Count > 0
            ? await _repository.ListProjectedByChunkedContainsAsync<ContactLabel, (Guid, Guid, string, string?), Guid>(
                contactIds,
                chunk => cl => chunk.Contains(cl.ContactId),
                cl => new ValueTuple<Guid, Guid, string, string?>(cl.ContactId, cl.Label.Id, cl.Label.Name, cl.Label.Color))
            : [];

        Dictionary<Guid, List<LabelDto>> labelsByContact = allContactLabels
            .GroupBy(cl => cl.ContactId)
            .ToDictionary(g => g.Key, g => g.Select(cl => new LabelDto { Id = cl.LabelId, Name = cl.Name, Color = cl.Color }).OrderBy(l => l.Name).ToList());

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
            nameof(Contact.SignificantDates),
            nameof(Contact.SignificantDates) + "." + nameof(SignificantDate.ReminderOffsets),
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

        List<Guid> relatedIds = relationships.Select(r => r.RelatedEntityId)
            .Concat(relatedTo.Select(r => r.EntityId))
            .Distinct()
            .ToList();

        List<Contact> relatedContacts = [];
        if (relatedIds.Count > 0)
        {
            // Optimization: Project only necessary fields for relationships display (Id, Name, Gender, IsPartial)
            // This avoids fetching all columns (including large text fields) for every related contact.
            relatedContacts = await _repository.ListProjectedByChunkedContainsAsync<Contact, Contact, Guid>(
                relatedIds,
                chunk => c => chunk.Contains(c.Id),
                c => new Contact
                {
                    Id = c.Id,
                    FirstName = c.FirstName,
                    LastName = c.LastName,
                    Gender = c.Gender,
                    IsPartial = c.IsPartial
                });
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
             nameof(Contact.SignificantDates) + "." + nameof(SignificantDate.ReminderOffsets),
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
        dto.Birthday = bday?.EventDate.ToDateTime(TimeOnly.MinValue);
        if (bday != null)
        {
            dto.RemindOnBirthday = bday.ReminderOffsets.Any(ro => ro.DaysBeforeEvent == 0 && ro.IsActive);
        }

        // Explicitly await the task to ensure Result is not accessed prematurely or incorrectly, and handle null result from ListAsync safely
        List<Attachment> attachments = await _repository.ListAsync<Attachment>(a => a.ContactId == id && a.AttachmentType == AttachmentTypes.ProfileImage);
        Attachment? profileAttachment = attachments.FirstOrDefault();

        if (profileAttachment != null)
        {
            dto.ProfileImageId = profileAttachment.Id;
        }

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
