using Microsoft.AspNetCore.Mvc;
using Rvnx.CRM.Core.Constants;
using Rvnx.CRM.Core.DTOs.Common;
using Rvnx.CRM.Core.DTOs.Base;
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
            if (entityId == Guid.Empty) return NotFound();

            var viewModel = new NoteFormViewModel
            {
                EntityId = entityId,
                EntityType = entityType,
                EntityName = await GetEntityName(entityId, entityType)
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(NoteFormViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                Note note = viewModel.ToEntity();
                await _repository.AddAsync(note);
                await _repository.SaveChangesAsync();
                return RedirectToEntity(note.ContactId ?? Guid.Empty, EntityTypes.Person);
            }

            viewModel.EntityName = await GetEntityName(viewModel.EntityId, viewModel.EntityType);
            return View(viewModel);
        }

        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null) return NotFound();

            Note? note = await _repository.GetByIdAsync<Note>(id.Value);
            if (note == null) return NotFound();

            var viewModel = new NoteFormViewModel
            {
                Id = note.Id,
                Title = note.Title,
                Value = note.Value,
                EntityId = note.ContactId ?? Guid.Empty,
                EntityType = EntityTypes.Person,
                EntityName = await GetEntityName(note.ContactId ?? Guid.Empty, EntityTypes.Person)
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, NoteFormViewModel viewModel)
        {
            if (id != viewModel.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    Note? existingNote = await _repository.GetByIdAsync<Note>(id);
                    if (existingNote == null) return NotFound();

                    existingNote.UpdateEntity(viewModel);

                    await _repository.UpdateAsync(existingNote);
                    await _repository.SaveChangesAsync();

                    return RedirectToEntity(existingNote.ContactId ?? Guid.Empty, EntityTypes.Person);
                }
                catch (Exception)
                {
                    if (!await _repository.ExistsAsync<Note>(viewModel.Id.Value)) return NotFound();
                    else throw;
                }
            }

            if (viewModel.EntityId != Guid.Empty)
            {
                viewModel.EntityName = await GetEntityName(viewModel.EntityId, viewModel.EntityType);
            }

            return View(viewModel);
        }

        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null) return NotFound();

            Note? note = await _repository.GetByIdAsync<Note>(id.Value);
            if (note == null) return NotFound();

            var viewModel = new NoteDeleteViewModel
            {
                Id = note.Id,
                Title = note.Title,
                Value = note.Value,
                EntityId = note.ContactId ?? Guid.Empty,
                EntityType = EntityTypes.Person,
                CreatedDate = note.CreatedDate,
                EntityName = await GetEntityName(note.ContactId ?? Guid.Empty, EntityTypes.Person)
            };
            return View(viewModel);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            Note? note = await _repository.GetByIdAsync<Note>(id);
            if (note != null)
            {
                Guid entityId = note.ContactId ?? Guid.Empty;
                string entityType = EntityTypes.Person;
                await _repository.DeleteAsync<Note>(id);
                await _repository.SaveChangesAsync();
                return RedirectToEntity(entityId, entityType);
            }
            return RedirectToAction("Index", "Home");
        }
    }
}
