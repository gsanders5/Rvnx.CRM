using Microsoft.EntityFrameworkCore;
using Rvnx.CRM.Core.Constants;
using Rvnx.CRM.Core.DTOs.Contact;
using Rvnx.CRM.Core.Enumerations;
using Rvnx.CRM.Core.Extensions;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models.Base;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Core.Models.Dates;

namespace Rvnx.CRM.Core.Services;

public class ContactManagementService(IRepository repository, IFileValidationService fileValidationService) : IContactManagementService
{
    private readonly IRepository _repository = repository;
    private readonly IFileValidationService _fileValidationService = fileValidationService;

    public async Task DeleteContactAsync(Guid contactId)
    {
        List<Rvnx.CRM.Core.Models.User> userWithSelfContact = await _repository.ListAsync<Rvnx.CRM.Core.Models.User>(u => u.SelfContactId == contactId);
        foreach (Rvnx.CRM.Core.Models.User user in userWithSelfContact)
        {
            user.SelfContactId = null;
            await _repository.UpdateAsync(user);
        }

        List<Relationship> userRelationships = await _repository.ListAsync<Relationship>(r =>
            (r.EntityId == contactId || r.RelatedEntityId == contactId) && r.EntityType == EntityTypes.Person);

        List<Guid> linkedContactIds = userRelationships
            .Select(r => r.EntityId == contactId ? r.RelatedEntityId : r.EntityId)
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
                    chunk => r => (chunk.Contains(r.EntityId) || chunk.Contains(r.RelatedEntityId)) && r.EntityType == EntityTypes.Person,
                    asNoTracking: false);

                HashSet<Guid> allInvolvedIds = allPartialRels
                    .Select(r => r.EntityId)
                    .Concat(allPartialRels.Select(r => r.RelatedEntityId))
                    .ToHashSet();

                List<Guid> potentialFullContactIds = allInvolvedIds.Except(partialContactIds).ToList();
                HashSet<Guid> confirmedFullContactIds = [];

                if (potentialFullContactIds.Count > 0)
                {
                    List<Contact> fullContacts = await _repository.ListByChunkedContainsAsync<Contact, Guid>(
                        potentialFullContactIds,
                        chunk => c => chunk.Contains(c.Id) && !c.IsPartial,
                        asNoTracking: true);
                    confirmedFullContactIds = fullContacts.Select(c => c.Id).ToHashSet();
                }

                Dictionary<Guid, List<Relationship>> relsByContactId = partialContactIds.ToDictionary(id => id, id => new List<Relationship>());

                foreach (Relationship rel in allPartialRels)
                {
                    if (relsByContactId.TryGetValue(rel.EntityId, out List<Relationship>? list1))
                    {
                        list1.Add(rel);
                    }
                    if (relsByContactId.TryGetValue(rel.RelatedEntityId, out List<Relationship>? list2))
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
                            Guid siblingId = rel.EntityId == partialContact.Id ? rel.RelatedEntityId : rel.EntityId;
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
        Contact? existingContact = await _repository.GetByIdAsync<Contact>(id);
        if (existingContact == null)
        {
            return ContactOperationResult.Failure($"Contact with ID {id} not found.");
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

        await _repository.UpdateAsync(existingContact);

        try
        {
            await _repository.SaveChangesAsync();
            return ContactOperationResult.Ok(id);
        }
        catch (DbUpdateConcurrencyException)
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
        foreach (Attachment existingAttachment in existingAttachments)
        {
            existingAttachment.AttachmentType = "General";
            await _repository.UpdateAsync(existingAttachment);
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
        // Note, Reminder, SignificantDate, Pet, ContactMethod, Fact, Address, Attachment, PhoneNumber
        // are now configured with Cascade Delete via ContactId foreign key.

        await DeleteRelatedEntitiesAsync<Relationship>(contactId);
        await _repository.DeleteAsync<Relationship>(r => r.RelatedEntityId == contactId && r.EntityType == EntityTypes.Person);
    }

    private async Task DeleteRelatedEntitiesAsync<T>(Guid contactId) where T : PolymorphicEntity
    {
        await _repository.DeleteAsync<T>(e => e.EntityId == contactId && e.EntityType == EntityTypes.Person);
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

}
