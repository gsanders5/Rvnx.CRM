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
                MaidenName = c.MaidenName,
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
                chunk => a =>
                    a.ContactId != null && a.AttachmentType == AttachmentTypes.ProfileImage &&
                    chunk.Contains(a.ContactId.Value),
                a => new ValueTuple<Guid, Guid>(a.ContactId!.Value, a.Id))
            : [];

        if (profileAttachments.Count > 0)
        {
            // Optimization: Use Dictionary with capacity and TryAdd instead of GroupBy().ToDictionary(..., First())
            // to avoid allocations of IGrouping structures and redundant list iterations.
            Dictionary<Guid, Guid> attachmentMap = new(profileAttachments.Count);
            foreach ((Guid ContactId, Guid AttachmentId) in profileAttachments)
            {
                attachmentMap.TryAdd(ContactId, AttachmentId);
            }

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
                cl => new ValueTuple<Guid, Guid, string, string?>(cl.ContactId, cl.Label.Id, cl.Label.Name,
                    cl.Label.Color))
            : [];

        // Optimization: Use Dictionary with capacity and foreach loop instead of GroupBy().ToDictionary(...)
        // to avoid allocations of IGrouping structures and redundant list iterations.
        Dictionary<Guid, List<LabelDto>> labelsByContact = new(allContactLabels.Count);
        foreach ((Guid ContactId, Guid LabelId, string Name, string? Color) in allContactLabels)
        {
            if (!labelsByContact.TryGetValue(ContactId, out List<LabelDto>? labelsList))
            {
                labelsList = [];
                labelsByContact.TryAdd(ContactId, labelsList);
            }
            labelsList.Add(new LabelDto { Id = LabelId, Name = Name, Color = Color });
        }
        foreach (List<LabelDto> list in labelsByContact.Values)
        {
            list.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));
        }

        foreach (ContactDto? dto in contactDtos)
        {
            if (dto != null && labelsByContact.TryGetValue(dto.Id, out List<LabelDto>? labels))
            {
                dto.Labels = labels;
            }
        }

        List<(Guid ContactId, DateOnly EventDate)> birthdayDates = contactIds.Count > 0
            ? await _repository.ListProjectedByChunkedContainsAsync<SignificantDate, (Guid, DateOnly), Guid>(
                contactIds,
                chunk => sd =>
                    sd.ContactId.HasValue && chunk.Contains(sd.ContactId.Value) &&
                    sd.Title == SignificantDateTitles.Birthday,
                sd => new ValueTuple<Guid, DateOnly>(sd.ContactId!.Value, sd.EventDate))
            : [];


        if (birthdayDates.Count > 0)
        {
            // Optimization: Use Dictionary with capacity and TryAdd instead of GroupBy().ToDictionary(..., First())
            // to avoid allocations of IGrouping structures and redundant list iterations.
            Dictionary<Guid, DateOnly> birthdayMap = new(birthdayDates.Count);
            foreach ((Guid ContactId, DateOnly EventDate) in birthdayDates)
            {
                birthdayMap.TryAdd(ContactId, EventDate);
            }

            foreach (ContactDto? dto in contactDtos)
            {
                if (dto != null && birthdayMap.TryGetValue(dto.Id, out DateOnly bday))
                {
                    dto.Birthday = bday.ToDateTime(TimeOnly.MinValue);
                }
            }
        }

        return contactDtos;
    }

    public async Task<ContactDetailDto?> GetContactDetailsAsync(Guid id)
    {
        // Optimization: Use ListAsNoTrackingAsync to avoid change tracking overhead for read-only operation
        List<Contact> contacts = await _repository.ListAsNoTrackingAsync<Contact>(c => c.Id == id && !c.IsPartial,
            default,
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

        // Optimization: Replace LINQ Select().Concat().Distinct().ToList() with a pre-sized HashSet and foreach loops
        // to avoid multiple intermediate enumerator allocations and dynamic array resizing.
        HashSet<Guid> relatedIdsSet = new(relationships.Count + relatedTo.Count);
        foreach (var r in relationships)
        {
            relatedIdsSet.Add(r.RelatedEntityId);
        }
        foreach (var r in relatedTo)
        {
            relatedIdsSet.Add(r.EntityId);
        }

        List<Guid> relatedIds = [.. relatedIdsSet];

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
                    MaidenName = c.MaidenName,
                    Gender = c.Gender,
                    IsPartial = c.IsPartial
                });
        }

        if (relatedContacts.Count > 0)
        {
            Dictionary<Guid, Contact> relatedMap = relatedContacts.ToDictionary(c => c.Id);

            foreach (Relationship rel in relationships)
            {
                if (relatedMap.TryGetValue(rel.RelatedEntityId, out Contact? related))
                {
                    rel.RelatedPerson = related;
                }
            }

            foreach (Relationship rel in relatedTo)
            {
                if (relatedMap.TryGetValue(rel.EntityId, out Contact? person))
                {
                    rel.Person = person;
                }
            }
        }

        contact.Relationships = relationships;
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

        contactDto.Labels = contact.ContactLabels.Select(cl => cl.Label).OrderBy(l => l.Name)
            .Select(l => new LabelDto { Id = l.Id, Name = l.Name, Color = l.Color }).ToList();

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
            MaidenName = contact.MaidenName,
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

        // Optimization: Use ListProjectedAsync to fetch only the Id of the ProfileImage attachment.
        // This avoids loading the entire Attachment entity (including ContentType and FileName) into memory.
        Guid profileImageId = (await _repository.ListProjectedAsync<Attachment, Guid>(
            a => a.ContactId == id && a.AttachmentType == AttachmentTypes.ProfileImage,
            a => a.Id))?.FirstOrDefault() ?? Guid.Empty;

        if (profileImageId != Guid.Empty)
        {
            dto.ProfileImageId = profileImageId;
        }

        List<Label> allLabels = await _repository.ListAsNoTrackingAsync<Label>(l => true) ?? [];

        dto.AllLabels = allLabels.OrderBy(l => l.Name)
            .Select(l => new LabelDto { Id = l.Id, Name = l.Name, Color = l.Color }).ToList();
        dto.AssignedLabelIds = contact.ContactLabels.Select(cl => cl.LabelId).ToList();

        return dto;
    }

    public async Task<bool> ContactExistsAsync(Guid id)
    {
        return await _repository.IsValidContactAsync(id);
    }
}