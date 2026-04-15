using Rvnx.CRM.Core.Constants;
using Rvnx.CRM.Core.DTOs.Contact;
using Rvnx.CRM.Core.Exceptions;
using Rvnx.CRM.Core.Extensions;
using Rvnx.CRM.Core.Helpers;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models;
using Rvnx.CRM.Core.Models.Contact;

namespace Rvnx.CRM.Infrastructure.Services;

public class ContactMethodService(IRepository repository) : IContactMethodService
{
    private readonly IRepository _repository = repository;

    public async Task<List<ContactMethodDto>> GetByContactAsync(Guid contactId)
    {
        List<ContactMethod> methods = await _repository.ListAsync<ContactMethod>(
            cm => cm.ContactId == contactId
        );
        return [.. methods.Select(cm => cm.ToDto())];
    }

    public async Task<OperationResult> CreateAsync(ContactMethodFormDto dto)
    {
        if (!await _repository.IsValidContactAsync(dto.EntityId))
        {
            return OperationResult.Failure("Contact not found.");
        }

        ContactMethod contactInfo = dto.ToEntity();
        contactInfo.Value = SocialMediaUrlNormalizer.Normalize(contactInfo.Type, contactInfo.Value);

        await _repository.AddAsync(contactInfo);
        await _repository.SaveChangesAsync();

        return OperationResult.Ok(contactInfo.ContactId ?? Guid.Empty, EntityTypes.Person);
    }

    public async Task<OperationResult> UpdateAsync(Guid id, ContactMethodFormDto dto)
    {
        try
        {
            ContactMethod? existingContactInfo = await _repository.GetByIdAsync<ContactMethod>(id);
            if (existingContactInfo == null || !await _repository.IsValidContactAsync(existingContactInfo.ContactId ?? Guid.Empty))
            {
                return OperationResult.Failure("Contact method not found.");
            }

            existingContactInfo.UpdateEntity(dto);
            existingContactInfo.Value = SocialMediaUrlNormalizer.Normalize(existingContactInfo.Type, existingContactInfo.Value);

            await _repository.UpdateAsync(existingContactInfo);
            await _repository.SaveChangesAsync();

            return OperationResult.Ok(existingContactInfo.ContactId ?? Guid.Empty, EntityTypes.Person);
        }
        catch (EntityConcurrencyException)
        {
            if (!await _repository.ExistsAsync<ContactMethod>(dto.Id ?? Guid.Empty))
            {
                return OperationResult.Failure("Contact method not found.");
            }
            throw;
        }
    }

    public async Task<OperationResult> DeleteAsync(Guid id)
    {
        List<Guid?> contactIds = await _repository.ListProjectedAsync<ContactMethod, Guid?>(
            cm => cm.Id == id,
            cm => cm.ContactId);

        if (contactIds.Count > 0)
        {
            Guid entityId = contactIds.FirstOrDefault() ?? Guid.Empty;
            string entityType = EntityTypes.Person;
            await _repository.DeleteAsync<ContactMethod>(cm => cm.Id == id);
            await _repository.SaveChangesAsync();
            return OperationResult.Ok(entityId, entityType);
        }
        return OperationResult.Failure("Contact method not found.");
    }

    public async Task<ContactMethodFormDto?> GetFormAsync(Guid id)
    {
        ContactMethod? contactInfo = await _repository.GetByIdAsync<ContactMethod>(id);

        return contactInfo == null || !await _repository.IsValidContactAsync(contactInfo.ContactId ?? Guid.Empty)
            ? null
            : new ContactMethodFormDto
            {
                Id = contactInfo.Id,
                Type = contactInfo.Type,
                Value = contactInfo.Value,
                Label = contactInfo.Label,
                EntityId = contactInfo.ContactId ?? Guid.Empty,
                EntityType = EntityTypes.Person
            };
    }

    public async Task<ContactMethodFormDto?> GetFormForCreateAsync(Guid entityId, string entityType)
    {
        return !await _repository.IsValidContactAsync(entityId) ? null : new ContactMethodFormDto { EntityId = entityId, EntityType = entityType };
    }

    public async Task<ContactMethod?> GetByIdAsync(Guid id)
    {
        ContactMethod? contactInfo = await _repository.GetByIdAsync<ContactMethod>(id);
        return contactInfo == null || !await _repository.IsValidContactAsync(contactInfo.ContactId ?? Guid.Empty) ? null : contactInfo;
    }
}