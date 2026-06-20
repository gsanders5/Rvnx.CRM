using Rvnx.CRM.Core.Constants;
using Rvnx.CRM.Core.DTOs.Contact;
using Rvnx.CRM.Core.Enumerations;
using Rvnx.CRM.Core.Extensions;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models.Activity;
using Rvnx.CRM.Core.Models.Base;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Core.Models.Dates;

namespace Rvnx.CRM.Core.Services;

public class ContactReadService(IRepository repository, IFavoriteService favoriteService) : IContactReadService
{
    private readonly IRepository _repository = repository;
    private readonly IFavoriteService _favoriteService = favoriteService;

    public async Task<List<ContactDto>> GetIndexDataAsync(bool showHidden)
    {
        // Optimization: Project directly to DTO to avoid fetching all columns and instantiating Contact entities.
        // showHidden=true also surfaces deceased — the toggle is a single "include hidden + deceased" gate.
        List<ContactDto> contactDtos = await _repository.ListProjectedAsync<Contact, ContactDto>(
            x => !x.IsPartial && (showHidden || (!x.IsHidden && !x.IsDeceased)),
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
                IsPartial = c.IsPartial,
                IsDeceased = c.IsDeceased,
                DateOfDeath = c.DateOfDeath
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
            foreach (var (ContactId, AttachmentId) in profileAttachments)
            {
                attachmentMap.TryAdd(ContactId, AttachmentId);
            }

            foreach (ContactDto dto in contactDtos)
            {
                if (attachmentMap.TryGetValue(dto.Id, out Guid attachmentId))
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
        foreach (var (contactId, labelId, labelName, color) in allContactLabels)
        {
            if (!labelsByContact.TryGetValue(contactId, out List<LabelDto>? labelsList))
            {
                labelsList = [];
                labelsByContact.TryAdd(contactId, labelsList);
            }
            labelsList.Add(new LabelDto { Id = labelId, Name = labelName, Color = color });
        }
        foreach (List<LabelDto> list in labelsByContact.Values)
        {
            list.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));
        }

        foreach (ContactDto dto in contactDtos)
        {
            if (labelsByContact.TryGetValue(dto.Id, out List<LabelDto>? labels))
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
            foreach (var (ContactId, EventDate) in birthdayDates)
            {
                birthdayMap.TryAdd(ContactId, EventDate);
            }

            foreach (ContactDto dto in contactDtos)
            {
                if (birthdayMap.TryGetValue(dto.Id, out DateOnly bday))
                {
                    dto.Birthday = bday.ToDateTime(TimeOnly.MinValue);
                }
            }
        }

        List<(Guid ContactId, DateTime ActivityDate)> activityDates = contactIds.Count > 0
            ? await _repository.ListProjectedByChunkedContainsAsync<ActivityContact, (Guid, DateTime), Guid>(
                contactIds,
                chunk => ac => chunk.Contains(ac.ContactId),
                ac => new ValueTuple<Guid, DateTime>(ac.ContactId, ac.Activity.ActivityDate))
            : [];

        if (activityDates.Count > 0)
        {
            Dictionary<Guid, DateTime> lastActivityMap = new(activityDates.Count);
            foreach (var (contactId, activityDate) in activityDates)
            {
                if (!lastActivityMap.TryGetValue(contactId, out DateTime existing) || activityDate > existing)
                {
                    lastActivityMap[contactId] = activityDate;
                }
            }

            foreach (ContactDto dto in contactDtos)
            {
                if (lastActivityMap.TryGetValue(dto.Id, out DateTime lastActivity))
                {
                    dto.LastActivityDate = lastActivity;
                }
            }
        }

        HashSet<Guid> favoriteIds = await _favoriteService.GetFavoriteContactIdsAsync();
        if (favoriteIds.Count > 0)
        {
            foreach (ContactDto dto in contactDtos)
            {
                dto.IsFavorite = favoriteIds.Contains(dto.Id);
            }
        }

        return contactDtos;
    }

    public async Task<ContactDetailDto?> GetContactDetailsAsync(Guid id)
    {
        // Optimization: Use ListAsNoTrackingAsync to avoid change tracking overhead for read-only operation
        List<Contact> contacts = await _repository.ListAsNoTrackingAsync<Contact>(c => c.Id == id && !c.IsPartial,
            default,
            nameof(Contact.PetContacts) + "." + nameof(PetContact.Pet),
            nameof(Contact.Notes),
            nameof(Contact.SignificantDates),
            nameof(Contact.SignificantDates) + "." + nameof(SignificantDate.ReminderOffsets),
            nameof(Contact.ContactMethods),
            nameof(Contact.Facts),
            nameof(Contact.Addresses),
            nameof(Contact.Attachments),
            nameof(Contact.ContactLabels) + "." + nameof(ContactLabel.Label),
            nameof(Contact.ActivityContacts) + "." + nameof(ActivityContact.Activity),
            nameof(Contact.ContactTasks),
            nameof(Contact.ImmichLink));

        Contact? contact = contacts.FirstOrDefault();
        if (contact == null)
        {
            return null;
        }

        // Relationships Optimization: Fetch both outgoing and incoming relationships in one query
        List<Relationship> allRelationships = await _repository.ListAsNoTrackingAsync<Relationship>(r =>
            r.ContactId == id || r.RelatedContactId == id);

        List<Relationship> relationships = allRelationships.Where(r => r.ContactId == id).ToList();
        List<Relationship> relatedTo = allRelationships.Where(r => r.RelatedContactId == id).ToList();

        // Optimization: Replace LINQ Select().Concat().Distinct().ToList() with a pre-sized HashSet and foreach loops
        // to avoid multiple intermediate enumerator allocations and dynamic array resizing.
        HashSet<Guid> relatedIdsSet = new(relationships.Count + relatedTo.Count);
        foreach (Relationship r in relationships)
        {
            relatedIdsSet.Add(r.RelatedContactId);
        }
        foreach (Relationship r in relatedTo)
        {
            relatedIdsSet.Add(r.ContactId);
        }

        // Co-fetch the introducer with related contacts so a single round-trip fills both lookups.
        if (contact.IntroducedByContactId.HasValue)
        {
            relatedIdsSet.Add(contact.IntroducedByContactId.Value);
        }
        List<Guid> contactLookupIds = [.. relatedIdsSet];

        List<Contact> relatedContacts = [];
        if (contactLookupIds.Count > 0)
        {
            // Optimization: Project only necessary fields for relationships display (Id, Name, Gender, IsPartial, IsDeceased)
            // This avoids fetching all columns (including large text fields) for every related contact.
            relatedContacts = await _repository.ListProjectedByChunkedContainsAsync<Contact, Contact, Guid>(
                contactLookupIds,
                chunk => c => chunk.Contains(c.Id),
                c => new Contact
                {
                    Id = c.Id,
                    FirstName = c.FirstName,
                    LastName = c.LastName,
                    MaidenName = c.MaidenName,
                    Gender = c.Gender,
                    IsPartial = c.IsPartial,
                    IsDeceased = c.IsDeceased
                });
        }

        Dictionary<Guid, Contact> relatedMap = relatedContacts.ToDictionary(c => c.Id);

        foreach (Relationship rel in relationships)
        {
            if (relatedMap.TryGetValue(rel.RelatedContactId, out Contact? related))
            {
                rel.RelatedPerson = related;
            }
        }

        foreach (Relationship rel in relatedTo)
        {
            if (relatedMap.TryGetValue(rel.ContactId, out Contact? person))
            {
                rel.Person = person;
            }
        }

        contact.Relationships = relationships;
        contact.RelatedTo = relatedTo;

        ContactDetailDto contactDto = contact.ToDetailDto();
        List<PetDto> petDtos = contact.PetContacts.Select(pc =>
        {
            PetDto petDto = pc.Pet.ToDto();
            petDto.ContactId = contact.Id;
            return petDto;
        }).ToList();

        // Enrich each pet with its co-owner list (excluding the contact being viewed) so the
        // Pets card can render a deceased indicator next to any deceased co-owner.
        // The PetContact include only loads the viewed contact's join rows, so a separate
        // query is needed to discover the other owners.
        List<Guid> petIds = petDtos.Select(p => p.Id).ToList();
        if (petIds.Count > 0)
        {
            List<(Guid PetId, Guid OwnerId)> ownerLinks =
                await _repository.ListProjectedByChunkedContainsAsync<PetContact, (Guid, Guid), Guid>(
                    petIds,
                    chunk => pc => chunk.Contains(pc.PetId),
                    pc => new ValueTuple<Guid, Guid>(pc.PetId, pc.ContactId));

            HashSet<Guid> ownerIds = [.. ownerLinks.Select(o => o.OwnerId).Where(oid => oid != id)];

            Dictionary<Guid, (string Name, bool IsDeceased)> ownerInfoMap = [];
            if (ownerIds.Count > 0)
            {
                List<(Guid Id, string Name, bool IsDeceased)> ownerInfos =
                    await _repository.ListProjectedByChunkedContainsAsync<Contact, (Guid, string, bool), Guid>(
                        [.. ownerIds],
                        chunk => c => chunk.Contains(c.Id),
                        c => new ValueTuple<Guid, string, bool>(
                            c.Id,
                            (c.FirstName + " " + (c.LastName ?? "")).Trim(),
                            c.IsDeceased));

                foreach (var (oId, name, isDeceased) in ownerInfos)
                {
                    ownerInfoMap.TryAdd(oId, (name, isDeceased));
                }
            }

            Dictionary<Guid, List<PetOwnerDto>> ownersByPet = [];
            foreach (var (petId, ownerId) in ownerLinks)
            {
                if (ownerId == id || !ownerInfoMap.TryGetValue(ownerId, out (string Name, bool IsDeceased) info))
                {
                    continue;
                }

                if (!ownersByPet.TryGetValue(petId, out List<PetOwnerDto>? owners))
                {
                    owners = [];
                    ownersByPet[petId] = owners;
                }
                owners.Add(new PetOwnerDto
                {
                    Id = ownerId,
                    FullName = info.Name,
                    IsDeceased = info.IsDeceased
                });
            }

            foreach (PetDto petDto in petDtos)
            {
                if (ownersByPet.TryGetValue(petDto.Id, out List<PetOwnerDto>? owners))
                {
                    petDto.Owners = [.. owners.OrderBy(o => o.FullName, StringComparer.Ordinal)];
                }
            }
        }

        contactDto.Pets = petDtos;

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

        List<ActivityDto> activityDtos = contact.ActivityContacts.Select(ac =>
        {
            ActivityDto activityDto = ac.Activity.ToDto();
            activityDto.ContactId = contact.Id;
            return activityDto;
        }).ToList();

        List<Guid> activityIds = activityDtos.Select(a => a.Id).ToList();

        if (activityIds.Count > 0)
        {
            List<(Guid ActivityId, Guid ContactId)> allParticipants =
                await _repository.ListProjectedByChunkedContainsAsync<ActivityContact, (Guid, Guid), Guid>(
                    activityIds,
                    chunk => ac => chunk.Contains(ac.ActivityId),
                    ac => new ValueTuple<Guid, Guid>(ac.ActivityId, ac.ContactId));

            HashSet<Guid> otherContactIds = [.. allParticipants
                .Select(p => p.ContactId)
                .Where(cid => cid != id)];

            // Project name + deceased flag for each participant so the Activities card can render
            // a deceased indicator inline without a second query. The chunked-Contains predicate
            // still rides on the same global tenancy filter as the rest of the repository.
            Dictionary<Guid, (string Name, bool IsDeceased)> contactInfoMap = [];
            if (otherContactIds.Count > 0)
            {
                List<(Guid Id, string Name, bool IsDeceased)> infos =
                    await _repository.ListProjectedByChunkedContainsAsync<Contact, (Guid, string, bool), Guid>(
                        [.. otherContactIds],
                        chunk => c => chunk.Contains(c.Id),
                        c => new ValueTuple<Guid, string, bool>(
                            c.Id,
                            (c.FirstName + " " + (c.LastName ?? "")).Trim(),
                            c.IsDeceased));

                foreach (var (cId, name, isDeceased) in infos)
                {
                    contactInfoMap.TryAdd(cId, (name, isDeceased));
                }
            }

            Dictionary<Guid, List<Guid>> participantsByActivity = [];
            foreach (var (activityId, contactId) in allParticipants)
            {
                if (!participantsByActivity.TryGetValue(activityId, out List<Guid>? list))
                {
                    list = [];
                    participantsByActivity[activityId] = list;
                }
                list.Add(contactId);
            }

            foreach (ActivityDto actDto in activityDtos)
            {
                if (participantsByActivity.TryGetValue(actDto.Id, out List<Guid>? pIds))
                {
                    List<Guid> others = pIds.Where(pid => pid != id && contactInfoMap.ContainsKey(pid)).ToList();
                    actDto.ContactIds = others;
                    actDto.ContactNames = others.Select(pid => contactInfoMap[pid].Name).ToList();
                    actDto.ContactIsDeceased = others.Select(pid => contactInfoMap[pid].IsDeceased).ToList();
                }
            }
        }

        contactDto.Activities = activityDtos;

        if (contact.IntroducedByContactId.HasValue
            && relatedMap.TryGetValue(contact.IntroducedByContactId.Value, out Contact? introducer))
        {
            contactDto.IntroducedByContactName = introducer.FullName;
        }

        return contactDto;
    }

    public async Task<ContactFormDto?> GetContactFormAsync(Guid id)
    {
        List<Contact> contacts = await _repository.ListAsNoTrackingAsync<Contact>(
            c => c.Id == id && !c.IsPartial,
            default,
            nameof(Contact.ContactMethods),
            nameof(Contact.SignificantDates) + "." + nameof(SignificantDate.ReminderOffsets),
            nameof(Contact.ContactLabels),
            nameof(Contact.ImmichLink));

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
            IsDeceased = contact.IsDeceased,
            DateOfDeath = contact.DateOfDeath,
            Pronouns = contact.Pronouns,
            Gender = contact.Gender,
            Religion = contact.Religion,
            HowWeMet = contact.HowWeMet,
            FirstMetOn = contact.FirstMetOn,
            IntroducedByContactId = contact.IntroducedByContactId,
            ImmichPersonId = contact.ImmichLink?.ImmichPersonId,
            ImmichPersonName = contact.ImmichLink?.ImmichPersonName,
            ImmichTagId = contact.ImmichLink?.ImmichTagId,
            ImmichTagValue = contact.ImmichLink?.ImmichTagValue
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

        dto.IntroducerCandidates = await GetIntroducerCandidatesAsync(id);

        return dto;
    }

    public async Task<bool> ContactExistsAsync(Guid id)
    {
        return await _repository.IsValidContactAsync(id);
    }

    public async Task<bool> HasRelationshipsAsync(Guid id)
    {
        int count = await _repository.CountAsync<Relationship>(r =>
            r.ContactId == id || r.RelatedContactId == id);
        return count > 0;
    }

    public async Task<List<(Guid Id, string FullName)>> GetContactNamesAsync(
        bool excludeDeceased = false,
        IEnumerable<Guid>? alwaysIncludeIds = null)
    {
        // Materialize to a HashSet so the predicate captures a stable, EF-translatable closure.
        HashSet<Guid> includeIds = alwaysIncludeIds is null ? [] : [.. alwaysIncludeIds];

        return await _repository.ListProjectedAsync<Contact, (Guid, string)>(
            c => !c.IsHidden && (!excludeDeceased || !c.IsDeceased || includeIds.Contains(c.Id)),
            c => new ValueTuple<Guid, string>(c.Id,
                c.IsPartial && c.IsDeceased
                    ? (c.FirstName + " " + (c.LastName ?? "")).Trim() + " (Partial Contact, Deceased)"
                    : c.IsPartial
                        ? (c.FirstName + " " + (c.LastName ?? "")).Trim() + " (Partial Contact)"
                        : c.IsDeceased
                            ? (c.FirstName + " " + (c.LastName ?? "")).Trim() + " (Deceased)"
                            : (c.FirstName + " " + (c.LastName ?? "")).Trim()));
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1304:Specify CultureInfo", Justification = "Parameterless ToLower() is required inside the expression tree — EF Core translates it to SQL lower(); culture-aware overloads are not translatable.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1311:Specify a culture or use an invariant version", Justification = "Parameterless ToLower() is required inside the expression tree — EF Core translates it to SQL lower(); culture-aware overloads are not translatable.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA1862:Use the 'StringComparison' method overloads to perform case-insensitive string comparisons", Justification = "Parameterless ToLower() is required inside the expression tree — EF Core translates it to SQL lower(); string.Equals(StringComparison) is not translatable.")]
    public async Task<List<ContactSelectItemDto>> FindContactsByNameAsync(string firstName, string? lastName)
    {
        string firstLower = firstName.Trim().ToLowerInvariant();
        string? lastLower = string.IsNullOrWhiteSpace(lastName) ? null : lastName.Trim().ToLowerInvariant();

        // Parameterless ToLower() is required inside the expression tree — EF Core translates it
        // to SQL lower(); culture-aware overloads and string.Equals(StringComparison) are not translatable.
        return await _repository.ListProjectedAsync<Contact, ContactSelectItemDto>(
            c => !c.IsHidden
                && c.FirstName.ToLower() == firstLower
                && (lastLower == null || (c.LastName != null && c.LastName.ToLower() == lastLower)),
            c => new ContactSelectItemDto
            {
                Id = c.Id,
                FullName = (c.FirstName + " " + (c.LastName ?? "")).Trim()
            });
    }

    public async Task<List<ContactSelectItemDto>> GetIntroducerCandidatesAsync(Guid? excludeContactId)
    {
        // Tenancy is enforced by the global query filter; only exclude self (when known) and partial contacts.
        List<ContactSelectItemDto> candidates = await _repository.ListProjectedAsync<Contact, ContactSelectItemDto>(
            c => !c.IsPartial && (excludeContactId == null || c.Id != excludeContactId),
            c => new ContactSelectItemDto
            {
                Id = c.Id,
                FullName = (c.FirstName + " " + (c.LastName ?? "")).Trim()
            }) ?? [];
        return [.. candidates.OrderBy(x => x.FullName)];
    }
}
