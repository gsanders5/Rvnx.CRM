using Rvnx.CRM.Core.DTOs.Base;
using Rvnx.CRM.Core.Exceptions;
using Rvnx.CRM.Core.Extensions;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models;
using Rvnx.CRM.Core.Models.Base;

namespace Rvnx.CRM.Infrastructure.Services;

public class NoteService(IRepository repository, IContactLookupService contactLookupService) : INoteService
{
    private readonly IRepository _repository = repository;
    private readonly IContactLookupService _contactLookupService = contactLookupService;

    public async Task<List<NoteDto>> GetByContactAsync(Guid contactId)
    {
        List<Note> notes = await _repository.ListAsync<Note>(
            n => n.ContactId == contactId
        );
        return [.. notes.Select(n => n.ToDto())];
    }

    public async Task<OperationResult> CreateAsync(NoteFormViewModel dto)
    {
        if (!await _repository.IsValidContactAsync(dto.ContactId))
        {
            return OperationResult.NotFound("Contact not found.");
        }

        Note note = dto.ToEntity();
        await _repository.AddAsync(note);
        await _repository.SaveChangesAsync();

        return OperationResult.Ok(note.ContactId ?? Guid.Empty);
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

            return OperationResult.Ok(existingNote.ContactId ?? Guid.Empty);
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
            Guid contactId = contactIds.FirstOrDefault() ?? Guid.Empty;
            await _repository.DeleteAsync<Note>(n => n.Id == id);
            await _repository.SaveChangesAsync();
            return OperationResult.Ok(contactId);
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
                IsFavorite = note.IsFavorite,
                ContactId = note.ContactId ?? Guid.Empty,
                ContactName = await _contactLookupService.GetContactNameAsync(note.ContactId ?? Guid.Empty)
            };
    }

    public async Task<NoteFormViewModel?> GetFormForCreateAsync(Guid contactId)
    {
        return contactId == Guid.Empty || !await _repository.IsValidContactAsync(contactId)
            ? null
            : new NoteFormViewModel
            {
                ContactId = contactId,
                ContactName = await _contactLookupService.GetContactNameAsync(contactId)
            };
    }

    public async Task<Note?> GetByIdAsync(Guid id)
    {
        Note? note = await _repository.GetByIdAsync<Note>(id);
        return note == null || !await _repository.IsValidContactAsync(note.ContactId ?? Guid.Empty) ? null : note;
    }

    public async Task<OperationResult> ToggleFavoriteAsync(Guid id)
    {
        Note? note = await _repository.GetByIdAsync<Note>(id);
        if (note == null || !await _repository.IsValidContactAsync(note.ContactId ?? Guid.Empty))
        {
            return OperationResult.NotFound("Note not found.");
        }

        note.IsFavorite = !note.IsFavorite;
        await _repository.UpdateAsync(note);
        await _repository.SaveChangesAsync();

        return OperationResult.Ok(note.ContactId ?? Guid.Empty);
    }
}
