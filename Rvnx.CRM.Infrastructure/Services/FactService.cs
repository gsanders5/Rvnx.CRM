using Rvnx.CRM.Core.DTOs.Contact;
using Rvnx.CRM.Core.Exceptions;
using Rvnx.CRM.Core.Extensions;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models;
using Rvnx.CRM.Core.Models.Contact;

namespace Rvnx.CRM.Infrastructure.Services;

public class FactService(IRepository repository) : IFactService
{
    private readonly IRepository _repository = repository;

    public async Task<List<FactDto>> GetByContactAsync(Guid contactId)
    {
        List<Fact> facts = await _repository.ListAsync<Fact>(
            f => f.ContactId == contactId
        );
        return [.. facts.Select(f => f.ToDto())];
    }

    public async Task<OperationResult> CreateAsync(FactFormDto dto)
    {
        if (!await _repository.IsValidContactAsync(dto.ContactId))
        {
            return OperationResult.NotFound("Contact not found.");
        }

        Fact fact = dto.ToEntity();
        await _repository.AddAsync(fact);
        await _repository.SaveChangesAsync();

        return OperationResult.Ok(fact.ContactId ?? Guid.Empty);
    }

    public async Task<OperationResult> UpdateAsync(Guid id, FactFormDto dto)
    {
        try
        {
            Fact? existingFact = await _repository.GetByIdAsync<Fact>(id);
            if (existingFact == null || !await _repository.IsValidContactAsync(existingFact.ContactId ?? Guid.Empty))
            {
                return OperationResult.NotFound("Fact not found.");
            }

            existingFact.UpdateEntity(dto);

            await _repository.UpdateAsync(existingFact);
            await _repository.SaveChangesAsync();

            return OperationResult.Ok(existingFact.ContactId ?? Guid.Empty);
        }
        catch (EntityConcurrencyException)
        {
            if (!await _repository.ExistsAsync<Fact>(dto.Id ?? Guid.Empty))
            {
                return OperationResult.NotFound("Fact not found.");
            }
            throw;
        }
    }

    public async Task<OperationResult> DeleteAsync(Guid id)
    {
        List<Guid?> contactIds = await _repository.ListProjectedAsync<Fact, Guid?>(
            f => f.Id == id,
            f => f.ContactId);

        if (contactIds.Count > 0)
        {
            Guid entityId = contactIds.FirstOrDefault() ?? Guid.Empty;

            await _repository.DeleteAsync<Fact>(f => f.Id == id);
            await _repository.SaveChangesAsync();

            return OperationResult.Ok(entityId);
        }
        return OperationResult.NotFound("Fact not found.");
    }

    public async Task<FactFormDto?> GetFormAsync(Guid id)
    {
        Fact? fact = await _repository.GetByIdAsync<Fact>(id);

        return fact == null || !await _repository.IsValidContactAsync(fact.ContactId ?? Guid.Empty)
            ? null
            : new FactFormDto
            {
                Id = fact.Id,
                Category = fact.Category,
                Value = fact.Value,
                ContactId = fact.ContactId ?? Guid.Empty
            };
    }

    public async Task<FactFormDto?> GetFormForCreateAsync(Guid contactId)
    {
        return !await _repository.IsValidContactAsync(contactId)
            ? null
            : new FactFormDto { ContactId = contactId };
    }

    public async Task<Fact?> GetByIdAsync(Guid id)
    {
        Fact? fact = await _repository.GetByIdAsync<Fact>(id);
        return fact == null || !await _repository.IsValidContactAsync(fact.ContactId ?? Guid.Empty) ? null : fact;
    }
}
