using Microsoft.AspNetCore.Mvc;
using Rvnx.CRM.Core.DTOs.Base;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Web.Controllers.Base;

namespace Rvnx.CRM.Web.Controllers
{
    public class NotesController(INoteService noteService, IRepository repository, IEntityService entityService) : RepositoryController(repository)
    {
        private readonly INoteService _noteService = noteService;
        private readonly IEntityService _entityService = entityService;

        public async Task<IActionResult> Create(Guid entityId, string entityType)
        {
            NoteFormViewModel? viewModel = await _noteService.GetFormForCreateAsync(entityId, entityType);

            if (viewModel == null)
            {
                // Replicate original error handling logic for consistency
                if (entityType != Rvnx.CRM.Core.Constants.EntityTypes.Person)
                {
                    return BadRequest("Only Person entities are supported.");
                }
                return NotFound();
            }

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(NoteFormViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                OperationResult result = await _noteService.CreateAsync(viewModel);
                if (result.Success)
                {
                    return RedirectToEntity(result.RedirectId, result.RedirectType);
                }

                if (result.ErrorMessage == "Only Person entities are supported.") return BadRequest(result.ErrorMessage);
                if (result.ErrorMessage == "Contact not found.") return NotFound();
            }

            // Re-populate view data if validation failed
            if (viewModel.EntityId != Guid.Empty)
            {
                viewModel.EntityName = await _entityService.GetEntityNameAsync(viewModel.EntityType, viewModel.EntityId);
            }
            return View(viewModel);
        }

        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            NoteFormViewModel? viewModel = await _noteService.GetFormAsync(id.Value);
            return viewModel == null ? NotFound() : View(viewModel);
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
                    OperationResult result = await _noteService.UpdateAsync(id, viewModel);
                    if (result.Success)
                    {
                        return RedirectToEntity(result.RedirectId, result.RedirectType);
                    }
                    if (result.ErrorMessage == "Note not found.") return NotFound();
                }
                catch (Exception)
                {
                    throw;
                }
            }

            if (viewModel.EntityId != Guid.Empty)
            {
                viewModel.EntityName = await _entityService.GetEntityNameAsync(viewModel.EntityType, viewModel.EntityId);
            }

            return View(viewModel);
        }

        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            Note? note = await _noteService.GetByIdAsync(id.Value);
            if (note == null)
            {
                return NotFound();
            }

            NoteDeleteViewModel viewModel = new()
            {
                Id = note.Id,
                Title = note.Title,
                Value = note.Value,
                EntityId = note.ContactId ?? Guid.Empty,
                EntityType = Rvnx.CRM.Core.Constants.EntityTypes.Person,
                CreatedDate = note.CreatedDate,
                EntityName = await _entityService.GetEntityNameAsync(Rvnx.CRM.Core.Constants.EntityTypes.Person, note.ContactId ?? Guid.Empty)
            };
            return View(viewModel);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            OperationResult result = await _noteService.DeleteAsync(id);
            if (result.Success)
            {
                return RedirectToEntity(result.RedirectId, result.RedirectType);
            }
            return RedirectToAction("Index", "Home");
        }
    }
}
