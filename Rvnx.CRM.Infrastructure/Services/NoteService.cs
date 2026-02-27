using Rvnx.CRM.Core.Constants;
using Rvnx.CRM.Core.DTOs.Base;
using Rvnx.CRM.Core.Extensions;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models;
using Rvnx.CRM.Core.Models.Base;
using Rvnx.CRM.Core.Models.Contact;

namespace Rvnx.CRM.Infrastructure.Services;

public class NoteService(IRepository repository, IEntityService entityService) : INoteService
{
    private readonly IRepository _repository = repository;
    private readonly IEntityService _entityService = entityService;

    public async Task<OperationResult> CreateAsync(NoteFormViewModel dto)
    {
        if (dto.EntityType != EntityTypes.Person)
        {
            return OperationResult.Failure("Only Person entities are supported.");
        }

        if (!await IsValidContactAsync(dto.EntityId))
        {
            return OperationResult.Failure("Contact not found.");
        }

        Note note = dto.ToEntity();
        await _repository.AddAsync(note);
        await _repository.SaveChangesAsync();

        return OperationResult.Ok(note.ContactId ?? Guid.Empty, EntityTypes.Person);
    }

    public async Task<OperationResult> UpdateAsync(Guid id, NoteFormViewModel dto)
    {
        try
        {
            Note? existingNote = await _repository.GetByIdAsync<Note>(id);
            if (existingNote == null || !await IsValidContactAsync(existingNote.ContactId ?? Guid.Empty))
            {
                return OperationResult.Failure("Note not found.");
            }

            existingNote.UpdateEntity(dto);

            await _repository.UpdateAsync(existingNote);
            await _repository.SaveChangesAsync();

            return OperationResult.Ok(existingNote.ContactId ?? Guid.Empty, EntityTypes.Person);
        }
        catch (Exception)
        {
            if (!await _repository.ExistsAsync<Note>(dto.Id ?? Guid.Empty))
            {
                return OperationResult.Failure("Note not found.");
            }
            throw;
        }
    }

    public async Task<OperationResult> DeleteAsync(Guid id)
    {
        Note? note = await _repository.GetByIdAsync<Note>(id);
        if (note != null)
        {
            Guid entityId = note.ContactId ?? Guid.Empty;
            string entityType = EntityTypes.Person;
            await _repository.DeleteAsync<Note>(id);
            await _repository.SaveChangesAsync();
            return OperationResult.Ok(entityId, entityType);
        }
        return OperationResult.Failure("Note not found.");
    }

    public async Task<NoteFormViewModel?> GetFormAsync(Guid id)
    {
        Note? note = await _repository.GetByIdAsync<Note>(id);

        return note == null || !await IsValidContactAsync(note.ContactId ?? Guid.Empty)
            ? null
            : new NoteFormViewModel
            {
                Id = note.Id,
                Title = note.Title,
                Value = note.Value,
                EntityId = note.ContactId ?? Guid.Empty,
                EntityType = EntityTypes.Person,
                EntityName = await _entityService.GetEntityNameAsync(EntityTypes.Person, note.ContactId ?? Guid.Empty)
            };
    }

    public async Task<NoteFormViewModel?> GetFormForCreateAsync(Guid entityId, string entityType)
    {
        return entityId == Guid.Empty || entityType != EntityTypes.Person
            ? null
            : !await IsValidContactAsync(entityId)
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
        return note == null || !await IsValidContactAsync(note.ContactId ?? Guid.Empty) ? null : note;
    }

    private async Task<bool> IsValidContactAsync(Guid id)
    {
        return id != Guid.Empty && await _repository.CountAsync<Contact>(c => c.Id == id && !c.IsPartial) > 0;
    }
}
