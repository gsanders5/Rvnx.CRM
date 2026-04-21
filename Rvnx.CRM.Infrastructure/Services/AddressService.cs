using Rvnx.CRM.Core.DTOs.Contact;
using Rvnx.CRM.Core.Enumerations;
using Rvnx.CRM.Core.Exceptions;
using Rvnx.CRM.Core.Extensions;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models;
using Rvnx.CRM.Core.Models.Contact;

namespace Rvnx.CRM.Infrastructure.Services;

public class AddressService(IRepository repository) : IAddressService
{
    private readonly IRepository _repository = repository;

    public async Task<List<AddressDto>> GetByContactAsync(Guid contactId)
    {
        List<Address> addresses = await _repository.ListAsync<Address>(
            a => a.ContactId == contactId
        );
        return [.. addresses.Select(a => a.ToDto())];
    }

    public async Task<OperationResult> CreateAsync(AddressFormDto dto)
    {
        if (!await _repository.IsValidContactAsync(dto.EntityId))
        {
            return OperationResult.NotFound("Contact not found.");
        }

        Address address = dto.ToEntity();
        await _repository.AddAsync(address);
        await _repository.SaveChangesAsync();

        return OperationResult.Ok(address.ContactId ?? Guid.Empty, EntityType.Person);
    }

    public async Task<OperationResult> UpdateAsync(Guid id, AddressFormDto dto)
    {
        try
        {
            Address? existing = await _repository.GetByIdAsync<Address>(id);
            if (existing == null || !await _repository.IsValidContactAsync(existing.ContactId ?? Guid.Empty))
            {
                return OperationResult.NotFound("Address not found.");
            }

            existing.UpdateEntity(dto);
            await _repository.UpdateAsync(existing);
            await _repository.SaveChangesAsync();

            return OperationResult.Ok(existing.ContactId ?? Guid.Empty, EntityType.Person);
        }
        catch (EntityConcurrencyException)
        {
            if (!await _repository.ExistsAsync<Address>(id))
            {
                return OperationResult.NotFound("Address not found.");
            }
            throw;
        }
    }

    public async Task<OperationResult> DeleteAsync(Guid id)
    {
        List<Guid?> contactIds = await _repository.ListProjectedAsync<Address, Guid?>(
            a => a.Id == id,
            a => a.ContactId);

        if (contactIds.Count > 0)
        {
            Guid entityId = contactIds.FirstOrDefault() ?? Guid.Empty;
            await _repository.DeleteAsync<Address>(a => a.Id == id);
            await _repository.SaveChangesAsync();
            return OperationResult.Ok(entityId, EntityType.Person);
        }

        return OperationResult.NotFound("Address not found.");
    }

    public async Task<AddressFormDto?> GetFormAsync(Guid id)
    {
        Address? address = await _repository.GetByIdAsync<Address>(id);
        return address == null || !await _repository.IsValidContactAsync(address.ContactId ?? Guid.Empty)
            ? null
            : new AddressFormDto
            {
                Id = address.Id,
                EntityId = address.ContactId ?? Guid.Empty,
                Line1 = address.Line1,
                Line2 = address.Line2,
                City = address.City,
                State = address.State,
                Zip = address.Zip,
                Country = address.Country,
                AddressType = address.AddressType
            };
    }

    public async Task<AddressFormDto?> GetFormForCreateAsync(Guid entityId)
    {
        return !await _repository.IsValidContactAsync(entityId)
            ? null
            : new AddressFormDto { EntityId = entityId };
    }

    public async Task<Address?> GetByIdAsync(Guid id)
    {
        Address? address = await _repository.GetByIdAsync<Address>(id);
        return address == null || !await _repository.IsValidContactAsync(address.ContactId ?? Guid.Empty) ? null : address;
    }
}
