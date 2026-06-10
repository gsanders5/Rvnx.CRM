using Rvnx.CRM.Core.Constants;
using Rvnx.CRM.Core.DTOs.Base;
using Rvnx.CRM.Core.DTOs.Contact;
using Rvnx.CRM.Core.Enumerations;
using Rvnx.CRM.Core.Exceptions;
using Rvnx.CRM.Core.Extensions;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models.Base;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Core.Models.Dates;

namespace Rvnx.CRM.Core.Services;

public class ContactManagementService(IRepository repository, IFileValidationService fileValidationService, ISelfContactService selfContactService) : IContactManagementService
{
    private readonly IRepository _repository = repository;
    private readonly IFileValidationService _fileValidationService = fileValidationService;
    private readonly ISelfContactService _selfContactService = selfContactService;

    public async Task DeleteContactAsync(Guid contactId)
    {
        // GetByIdAsync/DeleteAsync resolve by primary key via FindAsync, which bypasses the global
        // group query filter. Gate on the filtered ExistsAsync first so a caller cannot delete a
        // contact (and cascade its children) belonging to another group.
        if (!await _repository.ExistsAsync<Contact>(contactId))
        {
            return;
        }

        List<Rvnx.CRM.Core.Models.User> userWithSelfContact = await _repository.ListAsync<Rvnx.CRM.Core.Models.User>(u => u.SelfContactId == contactId);
        if (userWithSelfContact.Count > 0)
        {
            foreach (Rvnx.CRM.Core.Models.User user in userWithSelfContact)
            {
                user.SelfContactId = null;
            }
            await _repository.UpdateRangeAsync(userWithSelfContact);
        }

        List<Relationship> userRelationships = await _repository.ListAsync<Relationship>(r =>
            r.ContactId == contactId || r.RelatedContactId == contactId);

        List<Guid> linkedContactIds = userRelationships
            .Select(r => r.ContactId == contactId ? r.RelatedContactId : r.ContactId)
            .Distinct()
            .ToList();

        await DeleteContactDependenciesAsync(contactId);
        await _repository.DeleteAsync<Contact>(contactId);
        await _repository.SaveChangesAsync();

        if (linkedContactIds.Count > 0)
        {
            List<Contact> linkedPartialContacts = await _repository.ListByChunkedContainsAsync<Contact, Guid>(
                linkedContactIds,
                chunk => c => chunk.Contains(c.Id) && c.IsPartial,
                asNoTracking: false);

            if (linkedPartialContacts.Count > 0)
            {
                List<Guid> partialContactIds = linkedPartialContacts.Select(c => c.Id).ToList();
                List<Relationship> allPartialRels = await _repository.ListByChunkedContainsAsync<Relationship, Guid>(
                    partialContactIds,
                    chunk => r => chunk.Contains(r.ContactId) || chunk.Contains(r.RelatedContactId),
                    asNoTracking: false);

                // Optimization: avoid multiple iterations and LINQ enumerations by iterating once over the relationships
                HashSet<Guid> allInvolvedIds = new(allPartialRels.Count * 2);
                foreach (Relationship r in allPartialRels)
                {
                    allInvolvedIds.Add(r.ContactId);
                    allInvolvedIds.Add(r.RelatedContactId);
                }

                List<Guid> potentialFullContactIds = allInvolvedIds.Except(partialContactIds).ToList();
                HashSet<Guid> confirmedFullContactIds = [];

                if (potentialFullContactIds.Count > 0)
                {
                    List<Contact> fullContacts = await _repository.ListByChunkedContainsAsync<Contact, Guid>(
                        potentialFullContactIds,
                        chunk => c => chunk.Contains(c.Id) && !c.IsPartial,
                        asNoTracking: true);

                    // Optimization: Pre-allocate HashSet capacity and populate via foreach
                    // to avoid LINQ iterator state machine allocations and dynamic array resizing overhead.
                    confirmedFullContactIds = new HashSet<Guid>(fullContacts.Count);
                    foreach (Contact c in fullContacts)
                    {
                        confirmedFullContactIds.Add(c.Id);
                    }
                }

                Dictionary<Guid, List<Relationship>> relsByContactId = partialContactIds.ToDictionary(id => id, id => new List<Relationship>());

                foreach (Relationship rel in allPartialRels)
                {
                    if (relsByContactId.TryGetValue(rel.ContactId, out List<Relationship>? list1))
                    {
                        list1.Add(rel);
                    }
                    if (relsByContactId.TryGetValue(rel.RelatedContactId, out List<Relationship>? list2))
                    {
                        list2.Add(rel);
                    }
                }

                foreach (Contact partialContact in linkedPartialContacts)
                {
                    bool hasFullContactRelationship = false;

                    if (relsByContactId.TryGetValue(partialContact.Id, out List<Relationship>? myRels))
                    {
                        foreach (Relationship rel in myRels)
                        {
                            Guid siblingId = rel.ContactId == partialContact.Id ? rel.RelatedContactId : rel.ContactId;
                            if (confirmedFullContactIds.Contains(siblingId))
                            {
                                hasFullContactRelationship = true;
                                break;
                            }
                        }
                    }

                    if (!hasFullContactRelationship)
                    {
                        await DeleteContactDependenciesAsync(partialContact.Id);
                        await _repository.DeleteAsync<Contact>(partialContact.Id);
                    }
                }
            }
            await _repository.SaveChangesAsync();
        }
    }

    public async Task<BulkOperationResult> BulkDeleteAsync(IReadOnlyCollection<Guid> contactIds)
    {
        if (contactIds.Count == 0)
        {
            return BulkOperationResult.Ok(0);
        }

        Guid? selfContactId = await _selfContactService.GetSelfContactIdAsync();
        HashSet<Guid> targetIds = [];
        int skipped = 0;
        foreach (Guid id in contactIds)
        {
            // Skip the self-contact so a user can't accidentally delete themselves out of
            // their own dashboard, calendar, and reminders.
            if (selfContactId.HasValue && id == selfContactId.Value)
            {
                skipped++;
                continue;
            }
            targetIds.Add(id);
        }

        if (targetIds.Count == 0)
        {
            return BulkOperationResult.Ok(0, skipped);
        }

        List<Guid> existingIds = await _repository.ListProjectedByChunkedContainsAsync<Contact, Guid, Guid>(
            [.. targetIds],
            chunk => c => chunk.Contains(c.Id),
            c => c.Id);
        skipped += targetIds.Count - existingIds.Count;

        // Delegates to the single-row delete so partial-contact cascade cleanup and
        // self-contact FK detachment behave identically to the existing flow.
        foreach (Guid id in existingIds)
        {
            await DeleteContactAsync(id);
        }

        return BulkOperationResult.Ok(existingIds.Count, skipped);
    }

    public async Task<BulkOperationResult> BulkSetHiddenAsync(IReadOnlyCollection<Guid> contactIds, bool hidden)
    {
        if (contactIds.Count == 0)
        {
            return BulkOperationResult.Ok(0);
        }

        Guid? selfContactId = await _selfContactService.GetSelfContactIdAsync();
        HashSet<Guid> targetIds = [];
        int skipped = 0;
        foreach (Guid id in contactIds)
        {
            if (hidden && selfContactId.HasValue && id == selfContactId.Value)
            {
                skipped++;
                continue;
            }
            targetIds.Add(id);
        }

        if (targetIds.Count == 0)
        {
            return BulkOperationResult.Ok(0, skipped);
        }

        List<Contact> contacts = await _repository.ListByChunkedContainsAsync<Contact, Guid>(
            [.. targetIds],
            chunk => c => chunk.Contains(c.Id),
            asNoTracking: false);

        int successful = 0;
        foreach (Contact contact in contacts)
        {
            if (contact.IsHidden == hidden)
            {
                skipped++;
                continue;
            }
            contact.IsHidden = hidden;
            successful++;
        }

        skipped += targetIds.Count - contacts.Count;

        if (contacts.Count > 0)
        {
            await _repository.UpdateRangeAsync(contacts);
            await _repository.SaveChangesAsync();
        }

        return BulkOperationResult.Ok(successful, skipped);
    }

    public async Task<ContactOperationResult> CreateContactAsync(ContactFormDto contactDto)
    {
        Contact contact = contactDto.ToEntity();
        await _repository.AddAsync(contact);

        await ContactUpdateHelper.UpdateOrAddContactMethodAsync(_repository, contact.Id, ContactMethodType.Email, contactDto.Email, null);
        await ContactUpdateHelper.UpdateOrAddContactMethodAsync(_repository, contact.Id, ContactMethodType.Phone, contactDto.Phone, null);
        await ContactUpdateHelper.UpdateOrAddBirthdayAsync(_repository, contact.Id, contactDto.Birthday, null, contactDto.RemindOnBirthday);

        await _repository.SaveChangesAsync();
        return ContactOperationResult.Ok(contact.Id);
    }

    public async Task<ContactOperationResult> UpdateContactAsync(Guid id, ContactFormDto contactDto, Stream? imageStream, string? fileName, string? contentType)
    {
        // GetByIdAsync resolves by primary key via FindAsync, which bypasses the global group query
        // filter. Gate on the filtered ExistsAsync so a caller cannot update a contact in another group.
        if (!await _repository.ExistsAsync<Contact>(id))
        {
            return ContactOperationResult.NotFound();
        }

        Contact? existingContact = await _repository.GetByIdAsync<Contact>(id);
        if (existingContact == null)
        {
            return ContactOperationResult.Failure($"Contact with ID {id} not found.");
        }

        // Defense-in-depth: a user must never be able to mark their own self-contact deceased,
        // even by tampering with the form post or hitting the API directly. Doing so would
        // silently disable their reminders, dashboard, and calendar entries. Coerce the
        // deceased fields back to a safe default for the self-contact at the service boundary
        // so every caller (MVC, API PUT/PATCH, future jobs) is protected.
        Guid? selfContactId = await _selfContactService.GetSelfContactIdAsync();
        if (selfContactId.HasValue && selfContactId.Value == id)
        {
            contactDto.IsDeceased = false;
            contactDto.DateOfDeath = null;
        }

        existingContact.UpdateEntity(contactDto);

        ContactMethod? existingEmail = await GetPrimaryContactMethodAsync(id, ContactMethodType.Email);
        await ContactUpdateHelper.UpdateOrAddContactMethodAsync(_repository, id, ContactMethodType.Email, contactDto.Email, existingEmail);

        ContactMethod? existingPhone = await GetPrimaryContactMethodAsync(id, ContactMethodType.Phone);
        await ContactUpdateHelper.UpdateOrAddContactMethodAsync(_repository, id, ContactMethodType.Phone, contactDto.Phone, existingPhone);

        SignificantDate? existingBday = await GetBirthdayAsync(id);
        await ContactUpdateHelper.UpdateOrAddBirthdayAsync(_repository, id, contactDto.Birthday, existingBday, contactDto.RemindOnBirthday);

        ContactOperationResult imageResult = await HandleProfileImageUpdateAsync(id, imageStream, fileName, contentType);
        if (!imageResult.Success)
        {
            return imageResult;
        }

        await UpdateImmichLinkAsync(id, contactDto);

        await _repository.UpdateAsync(existingContact);

        try
        {
            await _repository.SaveChangesAsync();
            return ContactOperationResult.Ok(id);
        }
        catch (EntityConcurrencyException)
        {
            return !await _repository.ExistsAsync<Contact>(id)
                ? ContactOperationResult.NotFound()
                : ContactOperationResult.Failure("The contact was modified by another user. Please reload and try again.");
        }
    }

    public async Task<ContactOperationResult> UnsetProfilePhotoAsync(Guid contactId)
    {
        await ArchiveExistingProfilePhotoAsync(contactId);
        await _repository.SaveChangesAsync();
        return ContactOperationResult.Ok(contactId);
    }

    public async Task<ContactOperationResult> SetAttachmentAsProfilePhotoAsync(Guid contactId, Guid attachmentId)
    {
        Attachment? attachment = await _repository.GetByIdAsync<Attachment>(attachmentId);
        if (attachment == null)
        {
            return ContactOperationResult.NotFound();
        }

        if (attachment.ContactId != contactId)
        {
            return ContactOperationResult.NotFound();
        }

        if (!IsImage(attachment.ContentType, attachment.FileName))
        {
            return ContactOperationResult.Failure("Attachment is not an image.");
        }

        await ArchiveExistingProfilePhotoAsync(contactId);

        attachment.AttachmentType = AttachmentTypes.ProfileImage;
        await _repository.UpdateAsync(attachment);
        await _repository.SaveChangesAsync();

        return ContactOperationResult.Ok(contactId);
    }

    private async Task<ContactOperationResult> HandleProfileImageUpdateAsync(Guid contactId, Stream? imageStream, string? fileName, string? contentType)
    {
        if (imageStream == null || string.IsNullOrEmpty(fileName) || string.IsNullOrEmpty(contentType))
        {
            return ContactOperationResult.Ok(contactId);
        }

        string extension = Path.GetExtension(fileName).ToLowerInvariant();
        if (!_fileValidationService.IsImageExtension(extension) || !contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            return ContactOperationResult.Failure("Only image files (jpg, jpeg, png, gif) are allowed.");
        }

        using MemoryStream ms = new();
        await imageStream.CopyToAsync(ms);
        byte[] fileBytes = ms.ToArray();

        if (!_fileValidationService.IsValidImageSignature(fileBytes, extension))
        {
            return ContactOperationResult.Failure("Invalid file signature.");
        }

        await ArchiveExistingProfilePhotoAsync(contactId);

        Attachment attachment = new()
        {
            Id = Guid.NewGuid(),
            ContactId = contactId,
            AttachmentType = AttachmentTypes.ProfileImage,
            ContentType = contentType,
            FileName = fileName,
            AttachmentContent = new AttachmentContent
            {
                Content = fileBytes
            }
        };
        await _repository.AddAsync(attachment);

        return ContactOperationResult.Ok(contactId);
    }

    private async Task ArchiveExistingProfilePhotoAsync(Guid contactId)
    {
        List<Attachment> existingAttachments = await _repository.ListAsync<Attachment>(a => a.ContactId == contactId && a.AttachmentType == AttachmentTypes.ProfileImage);
        if (existingAttachments.Count > 0)
        {
            foreach (Attachment existingAttachment in existingAttachments)
            {
                existingAttachment.AttachmentType = AttachmentTypes.General;
            }
            await _repository.UpdateRangeAsync(existingAttachments);
        }
    }

    private bool IsImage(string contentType, string? fileName)
    {
        if (contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (!string.IsNullOrEmpty(fileName))
        {
            string ext = Path.GetExtension(fileName).ToLowerInvariant();
            return _fileValidationService.IsImageExtension(ext);
        }

        return false;
    }

    private async Task DeleteContactDependenciesAsync(Guid contactId)
    {
        // Child entities (Note, Reminder, SignificantDate, Pet, ContactMethod, Fact, Address,
        // Attachment, PhoneNumber) cascade-delete via the ContactId FK. Relationship rows are
        // deleted manually because they reference the contact from either ContactId or RelatedContactId.
        await _repository.DeleteAsync<Relationship>(r => r.ContactId == contactId || r.RelatedContactId == contactId);
    }

    public async Task<ContactOperationResult> DemoteToPartialAsync(Guid contactId)
    {
        // GetByIdAsync bypasses the global group query filter (FindAsync by key); gate on the
        // filtered ExistsAsync so a caller cannot demote a contact in another group.
        if (!await _repository.ExistsAsync<Contact>(contactId))
        {
            return ContactOperationResult.NotFound();
        }

        Contact? contact = await _repository.GetByIdAsync<Contact>(contactId);
        if (contact == null)
        {
            return ContactOperationResult.NotFound();
        }

        contact.IsPartial = true;
        await _repository.UpdateAsync(contact);
        await _repository.SaveChangesAsync();
        return ContactOperationResult.Ok(contactId);
    }

    private async Task<ContactMethod?> GetPrimaryContactMethodAsync(Guid contactId, ContactMethodType type)
    {
        List<ContactMethod> methods = await _repository.ListAsync<ContactMethod>(c => c.ContactId == contactId && c.Type == type);
        return methods.FirstOrDefault(e => e.Label == ContactMethodLabels.Primary) ?? methods.FirstOrDefault();
    }

    private async Task<SignificantDate?> GetBirthdayAsync(Guid contactId)
    {
        List<SignificantDate> bdays = await _repository.ListAsync<SignificantDate>(d => d.ContactId == contactId && d.Title == SignificantDateTitles.Birthday);
        return bdays.FirstOrDefault();
    }

    private async Task UpdateImmichLinkAsync(Guid contactId, ContactFormDto dto)
    {
        List<ContactImmichLink>? existing = await _repository.ListAsync<ContactImmichLink>(l => l.ContactId == contactId);
        ContactImmichLink? link = existing?.FirstOrDefault();

        bool hasAny = dto.ImmichPersonId.HasValue || dto.ImmichTagId.HasValue;

        if (!hasAny)
        {
            if (link != null)
            {
                await _repository.DeleteAsync(link);
            }
            return;
        }

        if (link == null)
        {
            await _repository.AddAsync(new ContactImmichLink
            {
                ContactId = contactId,
                ImmichPersonId = dto.ImmichPersonId,
                ImmichPersonName = dto.ImmichPersonName,
                ImmichTagId = dto.ImmichTagId,
                ImmichTagValue = dto.ImmichTagValue
            });
            return;
        }

        link.ImmichPersonId = dto.ImmichPersonId;
        link.ImmichPersonName = dto.ImmichPersonName;
        link.ImmichTagId = dto.ImmichTagId;
        link.ImmichTagValue = dto.ImmichTagValue;
        await _repository.UpdateAsync(link);
    }
}
