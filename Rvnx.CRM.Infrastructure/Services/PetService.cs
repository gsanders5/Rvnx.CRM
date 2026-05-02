using Rvnx.CRM.Core.DTOs.Contact;
using Rvnx.CRM.Core.Enumerations;
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
            return OperationResult.NotFound("Contact not found.");
        }

        // New pet ownership is forward-looking — refuse if the primary owner is deceased.
        if (!await _repository.IsLivingContactAsync(dto.EntityId))
        {
            return OperationResult.Failure("Cannot register a new pet for a deceased contact.");
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

        return OperationResult.Ok(dto.EntityId, EntityType.Person);
    }

    public async Task<OperationResult> UpdateAsync(Guid id, PetFormDto dto)
    {
        try
        {
            Pet? existingPet = await _repository.GetByIdWithIncludesAsync<Pet>(id, nameof(Pet.PetContacts));
            if (existingPet == null)
            {
                return OperationResult.NotFound("Pet not found.");
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

            return OperationResult.Ok(dto.EntityId, EntityType.Person);
        }
        catch (EntityConcurrencyException)
        {
            if (!await _repository.ExistsAsync<Pet>(dto.Id ?? Guid.Empty))
            {
                return OperationResult.NotFound("Pet not found.");
            }
            throw;
        }
    }

    public async Task<OperationResult> DeleteAsync(Guid id)
    {
        if (!await _repository.ExistsAsync<Pet>(id))
        {
            return OperationResult.NotFound("Pet not found.");
        }

        List<Guid> contactIds = await _repository.ListProjectedAsync<PetContact, Guid>(
            pc => pc.PetId == id,
            pc => pc.ContactId);

        Guid entityId = contactIds.FirstOrDefault();

        await _repository.DeleteAsync<Pet>(p => p.Id == id);
        await _repository.SaveChangesAsync();

        return OperationResult.Ok(entityId, EntityType.Person);
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
        // Forward-looking: refuse to render the create form when the primary owner is deceased.
        return !await _repository.IsLivingContactAsync(entityId)
            ? null
            : new PetFormDto { EntityId = entityId, ContactIds = [entityId] };
    }

    public async Task<Pet?> GetByIdAsync(Guid id)
    {
        return await _repository.GetByIdWithIncludesAsync<Pet>(id, nameof(Pet.PetContacts));
    }
}
