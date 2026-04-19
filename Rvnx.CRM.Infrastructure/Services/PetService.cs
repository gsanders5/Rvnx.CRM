using Rvnx.CRM.Core.Constants;
using Rvnx.CRM.Core.DTOs.Contact;
using Rvnx.CRM.Core.Exceptions;
using Rvnx.CRM.Core.Extensions;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models;
using Rvnx.CRM.Core.Models.Contact;

namespace Rvnx.CRM.Infrastructure.Services;

public class PetService(IRepository repository) : IPetService
{
    private readonly IRepository _repository = repository;

    public async Task<List<PetDto>> GetByContactAsync(Guid contactId)
    {
        List<PetContact> petContacts = await _repository.ListAsync<PetContact>(
            pc => pc.ContactId == contactId,
            default,
            nameof(PetContact.Pet)
        );
        return [.. petContacts.Select(pc => pc.Pet.ToDto())];
    }

    public async Task<OperationResult> CreateAsync(PetFormDto dto)
    {
        if (!await _repository.IsValidContactAsync(dto.EntityId))
        {
            return OperationResult.Failure("Contact not found.");
        }

        List<Guid> contactIds = dto.ContactIds.Count > 0 ? dto.ContactIds : [dto.EntityId];
        if (!contactIds.Contains(dto.EntityId))
        {
            contactIds.Add(dto.EntityId);
        }

        Pet pet = dto.ToEntity();
        await _repository.AddAsync(pet);

        List<PetContact> petContacts = contactIds.Select(contactId => new PetContact
        {
            PetId = pet.Id,
            ContactId = contactId
        }).ToList();
        await _repository.AddRangeAsync(petContacts);

        await _repository.SaveChangesAsync();

        return OperationResult.Ok(dto.EntityId, EntityTypes.Person);
    }

    public async Task<OperationResult> UpdateAsync(Guid id, PetFormDto dto)
    {
        try
        {
            Pet? existingPet = await _repository.GetByIdWithIncludesAsync<Pet>(id, nameof(Pet.PetContacts));
            if (existingPet == null)
            {
                return OperationResult.Failure("Pet not found.");
            }

            existingPet.UpdateEntity(dto);
            await _repository.UpdateAsync(existingPet);

            List<Guid> contactIds = dto.ContactIds.Count > 0 ? dto.ContactIds : [dto.EntityId];
            if (!contactIds.Contains(dto.EntityId))
            {
                contactIds.Add(dto.EntityId);
            }

            HashSet<Guid> desiredContactIds = [.. contactIds];
            HashSet<Guid> existingContactIds = [.. existingPet.PetContacts.Select(pc => pc.ContactId)];

            List<PetContact> toRemove = existingPet.PetContacts
                .Where(pc => !desiredContactIds.Contains(pc.ContactId))
                .ToList();
            if (toRemove.Count > 0)
            {
                await _repository.DeleteRangeAsync(toRemove);
            }

            List<Guid> toAdd = contactIds.Where(cid => !existingContactIds.Contains(cid)).ToList();
            if (toAdd.Count > 0)
            {
                await _repository.AddRangeAsync(toAdd.Select(contactId => new PetContact
                {
                    PetId = id,
                    ContactId = contactId
                }));
            }

            await _repository.SaveChangesAsync();

            return OperationResult.Ok(dto.EntityId, EntityTypes.Person);
        }
        catch (EntityConcurrencyException)
        {
            if (!await _repository.ExistsAsync<Pet>(dto.Id ?? Guid.Empty))
            {
                return OperationResult.Failure("Pet not found.");
            }
            throw;
        }
    }

    public async Task<OperationResult> DeleteAsync(Guid id)
    {
        Pet? pet = await _repository.GetByIdWithIncludesAsync<Pet>(id, nameof(Pet.PetContacts));
        if (pet != null)
        {
            Guid entityId = pet.PetContacts.Select(pc => pc.ContactId).FirstOrDefault();
            string entityType = EntityTypes.Person;
            await _repository.DeleteAsync<Pet>(id);
            await _repository.SaveChangesAsync();
            return OperationResult.Ok(entityId, entityType);
        }
        return OperationResult.Failure("Pet not found.");
    }

    public async Task<PetFormDto?> GetFormAsync(Guid id)
    {
        Pet? pet = await _repository.GetByIdWithIncludesAsync<Pet>(id, nameof(Pet.PetContacts));
        if (pet == null)
        {
            return null;
        }

        List<Guid> contactIds = pet.PetContacts.Select(pc => pc.ContactId).ToList();
        Guid entityId = contactIds.FirstOrDefault();

        return new PetFormDto
        {
            Id = pet.Id,
            EntityId = entityId,
            ContactIds = contactIds,
            Name = pet.Name,
            Species = pet.Species,
            Breed = pet.Breed,
            Birthday = pet.Birthday,
            Notes = pet.Notes
        };
    }

    public async Task<PetFormDto?> GetFormForCreateAsync(Guid entityId)
    {
        return !await _repository.IsValidContactAsync(entityId)
            ? null
            : new PetFormDto { EntityId = entityId, ContactIds = [entityId] };
    }

    public async Task<Pet?> GetByIdAsync(Guid id)
    {
        return await _repository.GetByIdWithIncludesAsync<Pet>(id, nameof(Pet.PetContacts));
    }
}