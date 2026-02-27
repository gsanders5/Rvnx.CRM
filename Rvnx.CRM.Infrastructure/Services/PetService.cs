using Rvnx.CRM.Core.Constants;
using Rvnx.CRM.Core.DTOs.Contact;
using Rvnx.CRM.Core.Extensions;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models;
using Rvnx.CRM.Core.Models.Contact;

namespace Rvnx.CRM.Infrastructure.Services;

public class PetService(IRepository repository) : IPetService
{
    private readonly IRepository _repository = repository;

    public async Task<OperationResult> CreateAsync(PetFormDto dto)
    {
        if (!await IsValidContactAsync(dto.EntityId))
        {
            return OperationResult.Failure("Contact not found.");
        }

        Pet pet = dto.ToEntity();
        await _repository.AddAsync(pet);
        await _repository.SaveChangesAsync();

        return OperationResult.Ok(pet.ContactId, EntityTypes.Person);
    }

    public async Task<OperationResult> UpdateAsync(Guid id, PetFormDto dto)
    {
        try
        {
            Pet? existingPet = await _repository.GetByIdAsync<Pet>(id);
            if (existingPet == null || !await IsValidContactAsync(existingPet.ContactId))
            {
                return OperationResult.Failure("Pet not found.");
            }

            existingPet.UpdateEntity(dto);

            await _repository.UpdateAsync(existingPet);
            await _repository.SaveChangesAsync();

            return OperationResult.Ok(existingPet.ContactId, EntityTypes.Person);
        }
        catch (Exception)
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
        Pet? pet = await _repository.GetByIdAsync<Pet>(id);
        if (pet != null)
        {
            Guid entityId = pet.ContactId;
            string entityType = EntityTypes.Person;
            await _repository.DeleteAsync<Pet>(id);
            await _repository.SaveChangesAsync();
            return OperationResult.Ok(entityId, entityType);
        }
        return OperationResult.Failure("Pet not found.");
    }

    public async Task<PetFormDto?> GetFormAsync(Guid id)
    {
        Pet? pet = await _repository.GetByIdAsync<Pet>(id);

        return pet == null || !await IsValidContactAsync(pet.ContactId)
            ? null
            : new PetFormDto
            {
                Id = pet.Id,
                EntityId = pet.ContactId,
                Name = pet.Name,
                Species = pet.Species,
                Breed = pet.Breed,
                Birthday = pet.Birthday,
                Notes = pet.Notes
            };
    }

    public async Task<PetFormDto?> GetFormForCreateAsync(Guid entityId)
    {
        return !await IsValidContactAsync(entityId) ? null : new PetFormDto { EntityId = entityId };
    }

    public async Task<Pet?> GetByIdAsync(Guid id)
    {
        Pet? pet = await _repository.GetByIdAsync<Pet>(id);
        return pet == null || !await IsValidContactAsync(pet.ContactId) ? null : pet;
    }

    private async Task<bool> IsValidContactAsync(Guid id)
    {
        return id != Guid.Empty && await _repository.CountAsync<Contact>(c => c.Id == id && !c.IsPartial) > 0;
    }
}
