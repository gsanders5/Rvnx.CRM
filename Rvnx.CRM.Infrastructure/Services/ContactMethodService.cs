using Rvnx.CRM.Core.Constants;
using Rvnx.CRM.Core.DTOs.Contact;
using Rvnx.CRM.Core.Extensions;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models;
using Rvnx.CRM.Core.Models.Contact;

namespace Rvnx.CRM.Infrastructure.Services;

public class ContactMethodService(IRepository repository) : IContactMethodService
{
    private readonly IRepository _repository = repository;

    public async Task<OperationResult> CreateAsync(ContactMethodFormDto dto)
    {
        if (!await IsValidContactAsync(dto.EntityId))
        {
            return OperationResult.Failure("Contact not found.");
        }

        ContactMethod contactInfo = dto.ToEntity();
        await _repository.AddAsync(contactInfo);
        await _repository.SaveChangesAsync();

        return OperationResult.Ok(contactInfo.ContactId ?? Guid.Empty, EntityTypes.Person);
    }

    public async Task<OperationResult> UpdateAsync(Guid id, ContactMethodFormDto dto)
    {
        try
        {
            ContactMethod? existingContactInfo = await _repository.GetByIdAsync<ContactMethod>(id);
            if (existingContactInfo == null || !await IsValidContactAsync(existingContactInfo.ContactId ?? Guid.Empty))
            {
                return OperationResult.Failure("Contact method not found.");
            }

            existingContactInfo.UpdateEntity(dto);

            await _repository.UpdateAsync(existingContactInfo);
            await _repository.SaveChangesAsync();

            return OperationResult.Ok(existingContactInfo.ContactId ?? Guid.Empty, EntityTypes.Person);
        }
        catch (Exception)
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
        ContactMethod? contactInfo = await _repository.GetByIdAsync<ContactMethod>(id);
        if (contactInfo != null)
        {
            Guid entityId = contactInfo.ContactId ?? Guid.Empty;
            string entityType = EntityTypes.Person;
            await _repository.DeleteAsync<ContactMethod>(id);
            await _repository.SaveChangesAsync();
            return OperationResult.Ok(entityId, entityType);
        }
        return OperationResult.Failure("Contact method not found.");
    }

    public async Task<ContactMethodFormDto?> GetFormAsync(Guid id)
    {
        ContactMethod? contactInfo = await _repository.GetByIdAsync<ContactMethod>(id);

        return contactInfo == null || !await IsValidContactAsync(contactInfo.ContactId ?? Guid.Empty)
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
        return !await IsValidContactAsync(entityId) ? null : new ContactMethodFormDto { EntityId = entityId, EntityType = entityType };
    }

    public async Task<ContactMethod?> GetByIdAsync(Guid id)
    {
        ContactMethod? contactInfo = await _repository.GetByIdAsync<ContactMethod>(id);
        return contactInfo == null || !await IsValidContactAsync(contactInfo.ContactId ?? Guid.Empty) ? null : contactInfo;
    }

    private async Task<bool> IsValidContactAsync(Guid id)
    {
        return id != Guid.Empty && await _repository.CountAsync<Contact>(c => c.Id == id && !c.IsPartial) > 0;
    }
}
