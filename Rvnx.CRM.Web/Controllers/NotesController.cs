using Microsoft.AspNetCore.Mvc;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models.Base;
using Rvnx.CRM.Web.Controllers.Base;

namespace Rvnx.CRM.Web.Controllers
{
    public class NotesController : RepositoryController
    {
        public NotesController(IRepository repository) : base(repository)
        {
        }

        public async Task<IActionResult> Create(Guid entityId, string entityType)
        {
            if (entityId == Guid.Empty || string.IsNullOrEmpty(entityType)) return NotFound();

            ViewData["EntityName"] = await GetEntityName(entityId, entityType);
            ViewData["EntityId"] = entityId;
            ViewData["EntityType"] = entityType;

            return View(new Note { EntityId = entityId, EntityType = entityType });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Title,Value,EntityId,EntityType")] Note note)
        {
            if (ModelState.IsValid)
            {
                note.Id = Guid.NewGuid();
                await _repository.AddAsync(note);
                await _repository.SaveChangesAsync();
                return RedirectToEntity(note.EntityId, note.EntityType);
            }

            ViewData["EntityName"] = await GetEntityName(note.EntityId, note.EntityType);
            ViewData["EntityId"] = note.EntityId;
            ViewData["EntityType"] = note.EntityType;
            return View(note);
        }

        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null) return NotFound();

            Note? note = await _repository.GetByIdAsync<Note>(id.Value);
            if (note == null) return NotFound();

            ViewData["EntityName"] = await GetEntityName(note.EntityId, note.EntityType);
            return View(note);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, [Bind("Id,Title,Value")] Note noteInput)
        {
            if (id != noteInput.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    // Fetch existing entity to preserve audit fields and prevent tampering
                    Note? existingNote = await _repository.GetByIdAsync<Note>(id);
                    if (existingNote == null) return NotFound();

                    // Only update user-editable fields
                    existingNote.Title = noteInput.Title;
                    existingNote.Value = noteInput.Value;
                    // EntityId, EntityType, CreatedDate, CreatedBy are preserved from existing entity

                    await _repository.UpdateAsync(existingNote);
                    await _repository.SaveChangesAsync();

                    return RedirectToEntity(existingNote.EntityId, existingNote.EntityType);
                }
                catch (Exception)
                {
                    if (!await _repository.ExistsAsync<Note>(noteInput.Id)) return NotFound();
                    else throw;
                }
            }

            // Re-fetch to get EntityId/EntityType for redirect and ViewData
            Note? note = await _repository.GetByIdAsync<Note>(id);
            if (note != null)
            {
                ViewData["EntityName"] = await GetEntityName(note.EntityId, note.EntityType);
                // Merge input values back for display
                note.Title = noteInput.Title;
                note.Value = noteInput.Value;
                return View(note);
            }

            return View(noteInput);
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