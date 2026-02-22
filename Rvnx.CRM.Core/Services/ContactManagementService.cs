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

        await DeleteContactDependenciesAsync(contactId);
        await _repository.DeleteAsync<Contact>(contactId);
        await _repository.SaveChangesAsync();
    }

    public async Task<ContactOperationResult> CreateContactAsync(ContactFormDto contactDto)
    {
        Contact contact = contactDto.ToEntity();
        await _repository.AddAsync(contact);
        await _repository.SaveChangesAsync();

        await UpdateOrAddContactMethod(contact.Id, ContactMethodType.Email, contactDto.Email, null);
        await UpdateOrAddContactMethod(contact.Id, ContactMethodType.Phone, contactDto.Phone, null);
        await UpdateOrAddBirthday(contact.Id, contactDto.Birthday, null);

        await _repository.SaveChangesAsync();
        return ContactOperationResult.Ok(contact.Id);
    }

    public async Task<ContactOperationResult> UpdateContactAsync(Guid id, ContactFormDto contactDto, Stream? imageStream, string? fileName, string? contentType)
    {
        Contact? existingContact = await _repository.GetByIdAsync<Contact>(id);
        if (existingContact == null) return ContactOperationResult.Failure($"Contact with ID {id} not found.");

        existingContact.UpdateEntity(contactDto);

        ContactMethod? existingEmail = await GetPrimaryContactMethodAsync(id, ContactMethodType.Email);
        await UpdateOrAddContactMethod(id, ContactMethodType.Email, contactDto.Email, existingEmail);

        ContactMethod? existingPhone = await GetPrimaryContactMethodAsync(id, ContactMethodType.Phone);
        await UpdateOrAddContactMethod(id, ContactMethodType.Phone, contactDto.Phone, existingPhone);

        SignificantDate? existingBday = await GetBirthdayAsync(id);
        await UpdateOrAddBirthday(id, contactDto.Birthday, existingBday);

        ContactOperationResult imageResult = await HandleProfileImageUpdateAsync(id, imageStream, fileName, contentType);
        if (!imageResult.Success)
        {
            return imageResult;
        }

        await _repository.UpdateAsync(existingContact);
        await _repository.SaveChangesAsync();

        return ContactOperationResult.Ok(id);
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

        List<Attachment> existingAttachments = await _repository.ListAsync<Attachment>(a => a.ContactId == contactId && a.AttachmentType == AttachmentTypes.ProfileImage);
        Attachment? existingAttachment = existingAttachments.FirstOrDefault();

        if (existingAttachment != null)
        {
            existingAttachment = await _repository.GetByIdWithIncludesAsync<Attachment>(existingAttachment.Id, "AttachmentContent");
            if (existingAttachment != null)
            {
                existingAttachment.AttachmentContent ??= new AttachmentContent { AttachmentId = existingAttachment.Id };
                existingAttachment.AttachmentContent.Content = fileBytes;

                existingAttachment.ContentType = contentType;
                existingAttachment.FileName = fileName;
                await _repository.UpdateAsync(existingAttachment);
            }
        }
        else
        {
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
        }

        return ContactOperationResult.Ok(contactId);
    }

    private async Task DeleteContactDependenciesAsync(Guid contactId)
    {
        // Note, Reminder, SignificantDate, Pet, ContactMethod, Fact, Address, Attachment, PhoneNumber
        // are now configured with Cascade Delete via ContactId foreign key.

        await DeleteRelatedEntitiesAsync<Relationship>(contactId);

        List<Relationship> relatedTo = await _repository.ListAsync<Relationship>(r => r.RelatedEntityId == contactId && r.EntityType == EntityTypes.Person);
        if (relatedTo.Count != 0) await _repository.DeleteRangeAsync(relatedTo);
    }

    private async Task DeleteRelatedEntitiesAsync<T>(Guid contactId) where T : PolymorphicEntity
    {
        List<T> entities = await _repository.ListAsync<T>(e => e.EntityId == contactId && e.EntityType == EntityTypes.Person);
        if (entities.Count != 0) await _repository.DeleteRangeAsync(entities);
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

    private async Task UpdateOrAddContactMethod(Guid contactId, ContactMethodType type, string? newValue, ContactMethod? existingMethod)
    {
        if (!string.IsNullOrEmpty(newValue))
        {
            if (existingMethod != null)
            {
                if (existingMethod.Value != newValue)
                {
                    existingMethod.Value = newValue;
                    await _repository.UpdateAsync(existingMethod);
                }
            }
            else
            {
                await _repository.AddAsync(new ContactMethod
                {
                    Id = Guid.NewGuid(),
                    ContactId = contactId,
                    Type = type,
                    Value = newValue,
                    Label = ContactMethodLabels.Primary
                });
            }
        }
        else if (existingMethod != null)
        {
            await _repository.DeleteAsync<ContactMethod>(existingMethod.Id);
        }
    }

    private async Task UpdateOrAddBirthday(Guid contactId, DateTime? newDate, SignificantDate? existingDate)
    {
        if (newDate.HasValue)
        {
            if (existingDate != null)
            {
                if (existingDate.Date != newDate.Value)
                {
                    existingDate.Date = newDate.Value;
                    await _repository.UpdateAsync(existingDate);
                }
            }
            else
            {
                await _repository.AddAsync(new SignificantDate
                {
                    Id = Guid.NewGuid(),
                    ContactId = contactId,
                    Title = SignificantDateTitles.Birthday,
                    Date = newDate.Value,
                    Description = "Birthday",
                    RemindMe = true,
                    EventFrequency = TimeSpan.FromDays(365)
                });
            }
        }
        else if (existingDate != null)
        {
            await _repository.DeleteAsync<SignificantDate>(existingDate.Id);
        }
    }
}
