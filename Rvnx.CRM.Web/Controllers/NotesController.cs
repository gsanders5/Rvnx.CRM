using Microsoft.AspNetCore.Mvc;
using Rvnx.CRM.Core.Constants;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models.Base;
using Rvnx.CRM.Core.Models.Business;
using Rvnx.CRM.Core.Models.Contact;
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
        public async Task<IActionResult> Edit(Guid id, [Bind("Id,Title,Value,EntityId,EntityType,CreatedDate,CreatedBy")] Note note)
        {
            if (id != note.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    await _repository.UpdateAsync(note);
                    await _repository.SaveChangesAsync();
                }
                catch (Exception)
                {
                    if (!await _repository.ExistsAsync<Note>(note.Id)) return NotFound();
                    else throw;
                }
                return RedirectToEntity(note.EntityId, note.EntityType);
            }

            ViewData["EntityName"] = await GetEntityName(note.EntityId, note.EntityType);
            return View(note);
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
