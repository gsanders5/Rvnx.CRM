using Microsoft.AspNetCore.Mvc;
using Rvnx.CRM.Core.DTOs.Common;
using Rvnx.CRM.Core.Extensions;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models.Base;
using Rvnx.CRM.Web.Controllers.Base;

namespace Rvnx.CRM.Web.Controllers
{
    public class NotesController(IRepository repository) : RepositoryController(repository)
    {
        public async Task<IActionResult> Create(Guid entityId, string entityType)
        {
            if (entityId == Guid.Empty || string.IsNullOrEmpty(entityType)) return NotFound();

            ViewData["EntityName"] = await GetEntityName(entityId, entityType);
            ViewData["EntityId"] = entityId;
            ViewData["EntityType"] = entityType;

            return View(new NoteFormDto { EntityId = entityId, EntityType = entityType });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(NoteFormDto noteDto)
        {
            if (ModelState.IsValid)
            {
                Note note = noteDto.ToEntity();
                await _repository.AddAsync(note);
                await _repository.SaveChangesAsync();
                return RedirectToEntity(note.EntityId, note.EntityType);
            }

            ViewData["EntityName"] = await GetEntityName(noteDto.EntityId, noteDto.EntityType);
            ViewData["EntityId"] = noteDto.EntityId;
            ViewData["EntityType"] = noteDto.EntityType;
            return View(noteDto);
        }

        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null) return NotFound();

            Note? note = await _repository.GetByIdAsync<Note>(id.Value);
            if (note == null) return NotFound();

            ViewData["EntityName"] = await GetEntityName(note.EntityId, note.EntityType);

            NoteFormDto dto = new()
            {
                Id = note.Id,
                Title = note.Title,
                Value = note.Value,
                EntityId = note.EntityId,
                EntityType = note.EntityType
            };

            return View(dto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, NoteFormDto noteDto)
        {
            if (id != noteDto.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    // Fetch existing entity to preserve audit fields and prevent tampering
                    Note? existingNote = await _repository.GetByIdAsync<Note>(id);
                    if (existingNote == null) return NotFound();

                    existingNote.UpdateEntity(noteDto);

                    await _repository.UpdateAsync(existingNote);
                    await _repository.SaveChangesAsync();

                    return RedirectToEntity(existingNote.EntityId, existingNote.EntityType);
                }
                catch (Exception)
                {
                    if (!await _repository.ExistsAsync<Note>(noteDto.Id.Value)) return NotFound();
                    else throw;
                }
            }

            // Re-fetch to get EntityId/EntityType for redirect and ViewData
            if (noteDto.EntityId != Guid.Empty)
            {
                ViewData["EntityName"] = await GetEntityName(noteDto.EntityId, noteDto.EntityType);
            }

            return View(noteDto);
        }

        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null) return NotFound();

            Note? note = await _repository.GetByIdAsync<Note>(id.Value);
            if (note == null) return NotFound();

            ViewData["EntityName"] = await GetEntityName(note.EntityId, note.EntityType);
            return View(note);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            Note? note = await _repository.GetByIdAsync<Note>(id);
            if (note != null)
            {
                Guid entityId = note.EntityId;
                string entityType = note.EntityType;
                await _repository.DeleteAsync<Note>(id);
                await _repository.SaveChangesAsync();
                return RedirectToEntity(entityId, entityType);
            }
            return RedirectToAction("Index", "Home");
        }
    }
}