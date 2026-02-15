using Microsoft.AspNetCore.Mvc;
using Rvnx.CRM.Core.Constants;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models.Base;
using Rvnx.CRM.Core.Models.Business;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Web.Controllers.Base;

namespace Rvnx.CRM.Web.Controllers
{
    public class NotesController : AuthorizedController
    {
        private readonly IRepository _repository;

        public NotesController(IRepository repository)
        {
            _repository = repository;
        }

        private async Task<string> GetEntityName(Guid id, string type)
        {
            if (type == EntityTypes.Person)
            {
                Contact? p = await _repository.GetByIdAsync<Contact>(id);
                return p?.FullName ?? "Unknown Person";
            }
            else if (type == EntityTypes.Company)
            {
                Employer? c = await _repository.GetByIdAsync<Employer>(id);
                return c?.CompanyName ?? "Unknown Company";
            }
            return "Unknown Entity";
        }

        private IActionResult RedirectToEntity(Guid id, string type)
        {
            if (type == EntityTypes.Person)
            {
                return RedirectToAction("Details", "Contacts", new { id });
            }
            // Add other types here
            return RedirectToAction("Index", "Home");
        }

        // GET: Notes/Create
        public async Task<IActionResult> Create(Guid entityId, string entityType)
        {
            if (entityId == Guid.Empty || string.IsNullOrEmpty(entityType)) return NotFound();

            ViewData["EntityName"] = await GetEntityName(entityId, entityType);
            ViewData["EntityId"] = entityId;
            ViewData["EntityType"] = entityType;

            return View(new Note { EntityId = entityId, EntityType = entityType });
        }

        // POST: Notes/Create
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

        // GET: Notes/Edit/5
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null) return NotFound();

            Note? note = await _repository.GetByIdAsync<Note>(id.Value);
            if (note == null) return NotFound();

            ViewData["EntityName"] = await GetEntityName(note.EntityId, note.EntityType);
            return View(note);
        }

        // POST: Notes/Edit/5
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

        // GET: Notes/Delete/5
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null) return NotFound();

            Note? note = await _repository.GetByIdAsync<Note>(id.Value);
            if (note == null) return NotFound();

            ViewData["EntityName"] = await GetEntityName(note.EntityId, note.EntityType);
            return View(note);
        }

        // POST: Notes/Delete/5
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
