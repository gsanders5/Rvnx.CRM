using Microsoft.AspNetCore.Mvc;
using Rvnx.CRM.Core.Constants;
using Rvnx.CRM.Core.DTOs.Base;
using Rvnx.CRM.Core.Extensions;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models.Base;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Web.Controllers.Base;

namespace Rvnx.CRM.Web.Controllers
{
    public class NotesController(IRepository repository, IEntityService entityService) : RepositoryController(repository)
    {
        public async Task<IActionResult> Create(Guid entityId, string entityType)
        {
            if (entityId == Guid.Empty)
            {
                return NotFound();
            }

            if (entityType != EntityTypes.Person)
            {
                return BadRequest("Only Person entities are supported.");
            }

            // Sentinel: Verify entity existence and access rights to prevent IDOR
            if (!await IsValidContactAsync(entityId))
            {
                return NotFound();
            }

            NoteFormViewModel viewModel = new()
            {
                EntityId = entityId,
                EntityType = entityType,
                EntityName = await entityService.GetEntityNameAsync(entityType, entityId)
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(NoteFormViewModel viewModel)
        {
            if (viewModel.EntityType != EntityTypes.Person)
            {
                return BadRequest("Only Person entities are supported.");
            }

            // Sentinel: Verify entity existence and access rights to prevent IDOR
            if (!await IsValidContactAsync(viewModel.EntityId))
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                Note note = viewModel.ToEntity();
                await Repository.AddAsync(note);
                await Repository.SaveChangesAsync();
                return RedirectToEntity(note.ContactId ?? Guid.Empty, EntityTypes.Person);
            }

            viewModel.EntityName = await entityService.GetEntityNameAsync(viewModel.EntityType, viewModel.EntityId);
            return View(viewModel);
        }

        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            Note? note = await Repository.GetByIdAsync<Note>(id.Value);
            if (note == null || !await IsValidContactAsync(note.ContactId ?? Guid.Empty))
            {
                return NotFound();
            }

            NoteFormViewModel viewModel = new()
            {
                Id = note.Id,
                Title = note.Title,
                Value = note.Value,
                EntityId = note.ContactId ?? Guid.Empty,
                EntityType = EntityTypes.Person,
                EntityName = await entityService.GetEntityNameAsync(EntityTypes.Person, note.ContactId ?? Guid.Empty)
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, NoteFormViewModel viewModel)
        {
            if (id != viewModel.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    Note? existingNote = await Repository.GetByIdAsync<Note>(id);
                    if (existingNote == null || !await IsValidContactAsync(existingNote.ContactId ?? Guid.Empty))
                    {
                        return NotFound();
                    }

                    existingNote.UpdateEntity(viewModel);

                    await Repository.UpdateAsync(existingNote);
                    await Repository.SaveChangesAsync();

                    return RedirectToEntity(existingNote.ContactId ?? Guid.Empty, EntityTypes.Person);
                }
                catch (Exception)
                {
                    if (!await Repository.ExistsAsync<Note>(viewModel.Id.Value))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            if (viewModel.EntityId != Guid.Empty)
            {
                viewModel.EntityName = await entityService.GetEntityNameAsync(viewModel.EntityType, viewModel.EntityId);
            }

            return View(viewModel);
        }

        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            Note? note = await Repository.GetByIdAsync<Note>(id.Value);
            if (note == null || !await IsValidContactAsync(note.ContactId ?? Guid.Empty))
            {
                return NotFound();
            }

            NoteDeleteViewModel viewModel = new()
            {
                Id = note.Id,
                Title = note.Title,
                Value = note.Value,
                EntityId = note.ContactId ?? Guid.Empty,
                EntityType = EntityTypes.Person,
                CreatedDate = note.CreatedDate,
                EntityName = await entityService.GetEntityNameAsync(EntityTypes.Person, note.ContactId ?? Guid.Empty)
            };
            return View(viewModel);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            Note? note = await Repository.GetByIdAsync<Note>(id);
            if (note != null)
            {
                Guid entityId = note.ContactId ?? Guid.Empty;
                string entityType = EntityTypes.Person;
                await Repository.DeleteAsync<Note>(id);
                await Repository.SaveChangesAsync();
                return RedirectToEntity(entityId, entityType);
            }
            return RedirectToAction("Index", "Home");
        }
    }
}
