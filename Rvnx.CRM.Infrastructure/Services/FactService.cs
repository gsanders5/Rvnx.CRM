using Rvnx.CRM.Core.Constants;
using Rvnx.CRM.Core.DTOs.Contact;
using Rvnx.CRM.Core.Extensions;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models;
using Rvnx.CRM.Core.Models.Contact;

namespace Rvnx.CRM.Infrastructure.Services;

public class FactService(IRepository repository) : IFactService
{
    private readonly IRepository _repository = repository;

    public async Task<OperationResult> CreateAsync(FactFormDto dto)
    {
        if (!await IsValidContactAsync(dto.EntityId))
        {
            return OperationResult.Failure("Contact not found.");
        }

        Fact fact = dto.ToEntity();
        await _repository.AddAsync(fact);
        await _repository.SaveChangesAsync();

        return OperationResult.Ok(fact.ContactId ?? Guid.Empty, EntityTypes.Person);
    }

    public async Task<OperationResult> UpdateAsync(Guid id, FactFormDto dto)
    {
        try
        {
            Fact? existingFact = await _repository.GetByIdAsync<Fact>(id);
            if (existingFact == null || !await IsValidContactAsync(existingFact.ContactId ?? Guid.Empty))
            {
                return OperationResult.Failure("Fact not found.");
            }

            existingFact.UpdateEntity(dto);

            await _repository.UpdateAsync(existingFact);
            await _repository.SaveChangesAsync();

            return OperationResult.Ok(existingFact.ContactId ?? Guid.Empty, EntityTypes.Person);
        }
        catch (Exception)
        {
            if (!await _repository.ExistsAsync<Fact>(dto.Id ?? Guid.Empty))
            {
                return OperationResult.Failure("Fact not found.");
            }
            throw;
        }
    }

    public async Task<OperationResult> DeleteAsync(Guid id)
    {
        Fact? fact = await _repository.GetByIdAsync<Fact>(id);
        if (fact != null)
        {
            Guid entityId = fact.ContactId ?? Guid.Empty;
            string entityType = EntityTypes.Person;
            await _repository.DeleteAsync<Fact>(id);
            await _repository.SaveChangesAsync();
            return OperationResult.Ok(entityId, entityType);
        }
        return OperationResult.Failure("Fact not found.");
    }

    public async Task<FactFormDto?> GetFormAsync(Guid id)
    {
        Fact? fact = await _repository.GetByIdAsync<Fact>(id);

        return fact == null || !await IsValidContactAsync(fact.ContactId ?? Guid.Empty)
            ? null
            : new FactFormDto
            {
                Id = fact.Id,
                Category = fact.Category,
                Value = fact.Value,
                EntityId = fact.ContactId ?? Guid.Empty,
                EntityType = EntityTypes.Person
            };
    }

    public async Task<FactFormDto?> GetFormForCreateAsync(Guid entityId, string entityType)
    {
        return !await IsValidContactAsync(entityId) ? null : new FactFormDto { EntityId = entityId, EntityType = entityType };
    }

    public async Task<Fact?> GetByIdAsync(Guid id)
    {
        Fact? fact = await _repository.GetByIdAsync<Fact>(id);
        return fact == null || !await IsValidContactAsync(fact.ContactId ?? Guid.Empty) ? null : fact;
    }

    private async Task<bool> IsValidContactAsync(Guid id)
    {
        return id != Guid.Empty && await _repository.CountAsync<Contact>(c => c.Id == id && !c.IsPartial) > 0;
    }
}
