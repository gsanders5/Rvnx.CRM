using Rvnx.CRM.Core.DTOs.Base;
using Rvnx.CRM.Core.Enumerations;
using Rvnx.CRM.Core.Exceptions;
using Rvnx.CRM.Core.Extensions;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models;
using Rvnx.CRM.Core.Models.Base;

namespace Rvnx.CRM.Infrastructure.Services;

public class NoteService(IRepository repository, IEntityService entityService) : INoteService
{
    private readonly IRepository _repository = repository;
    private readonly IEntityService _entityService = entityService;

    public async Task<List<NoteDto>> GetByContactAsync(Guid contactId)
    {
        List<Note> notes = await _repository.ListAsync<Note>(
            n => n.ContactId == contactId
        );
        return [.. notes.Select(n => n.ToDto())];
    }

    public async Task<OperationResult> CreateAsync(NoteFormViewModel dto)
    {
        if (dto.EntityType != EntityType.Person)
        {
            return OperationResult.Failure("Only Person entities are supported.");
        }

        if (!await _repository.IsValidContactAsync(dto.EntityId))
        {
            return OperationResult.NotFound("Contact not found.");
        }

        Note note = dto.ToEntity();
        await _repository.AddAsync(note);
        await _repository.SaveChangesAsync();

        return OperationResult.Ok(note.ContactId ?? Guid.Empty, EntityType.Person);
    }

    public async Task<OperationResult> UpdateAsync(Guid id, NoteFormViewModel dto)
    {
        try
        {
            Note? existingNote = await _repository.GetByIdAsync<Note>(id);
            if (existingNote == null || !await _repository.IsValidContactAsync(existingNote.ContactId ?? Guid.Empty))
            {
                return OperationResult.NotFound("Note not found.");
            }

            existingNote.UpdateEntity(dto);

            await _repository.UpdateAsync(existingNote);
            await _repository.SaveChangesAsync();

            return OperationResult.Ok(existingNote.ContactId ?? Guid.Empty, EntityType.Person);
        }
        catch (EntityConcurrencyException)
        {
            if (!await _repository.ExistsAsync<Note>(dto.Id ?? Guid.Empty))
            {
                return OperationResult.NotFound("Note not found.");
            }
            throw;
        }
    }

    public async Task<OperationResult> DeleteAsync(Guid id)
    {
        List<Guid?> contactIds = await _repository.ListProjectedAsync<Note, Guid?>(
            n => n.Id == id,
            n => n.ContactId);

        if (contactIds.Count > 0)
        {
            Guid entityId = contactIds.FirstOrDefault() ?? Guid.Empty;
            await _repository.DeleteAsync<Note>(n => n.Id == id);
            await _repository.SaveChangesAsync();
            return OperationResult.Ok(entityId, EntityType.Person);
        }
        return OperationResult.NotFound("Note not found.");
    }

    public async Task<NoteFormViewModel?> GetFormAsync(Guid id)
    {
        Note? note = await _repository.GetByIdAsync<Note>(id);

        return note == null || !await _repository.IsValidContactAsync(note.ContactId ?? Guid.Empty)
            ? null
            : new NoteFormViewModel
            {
                Id = note.Id,
                Title = note.Title,
                Value = note.Value,
                EntityId = note.ContactId ?? Guid.Empty,
                EntityType = EntityType.Person,
                EntityName = await _entityService.GetEntityNameAsync(EntityType.Person, note.ContactId ?? Guid.Empty)
            };
    }

    public async Task<NoteFormViewModel?> GetFormForCreateAsync(Guid entityId, EntityType entityType)
    {
        return entityId == Guid.Empty || entityType != EntityType.Person
            ? null
            : !await _repository.IsValidContactAsync(entityId)
            ? null
            : new NoteFormViewModel
            {
                EntityId = entityId,
                EntityType = entityType,
                EntityName = await _entityService.GetEntityNameAsync(entityType, entityId)
            };
    }

    public async Task<Note?> GetByIdAsync(Guid id)
    {
        Note? note = await _repository.GetByIdAsync<Note>(id);
        return note == null || !await _repository.IsValidContactAsync(note.ContactId ?? Guid.Empty) ? null : note;
    }
}