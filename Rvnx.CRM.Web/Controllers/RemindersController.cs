using Microsoft.AspNetCore.Mvc;
using Rvnx.CRM.Core.DTOs.Dates;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models;
using Rvnx.CRM.Web.Controllers.Base;

namespace Rvnx.CRM.Web.Controllers
{
    public class RemindersController(IReminderService reminderService, IRepository repository, IEntityService entityService) : RepositoryController(repository)
    {
        private readonly IReminderService _reminderService = reminderService;
        private readonly IEntityService _entityService = entityService;

        public async Task<IActionResult> Create(Guid entityId, string entityType)
        {
            ReminderFormViewModel? viewModel = await _reminderService.GetFormForCreateAsync(entityId, entityType);
            return viewModel == null ? NotFound() : View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ReminderFormViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                OperationResult result = await _reminderService.CreateAsync(viewModel);
                if (result.Success)
                {
                    return RedirectToEntity(result.RedirectId, result.RedirectType);
                }
                if (result.ErrorMessage == "Contact not found.")
                {
                    return NotFound();
                }
            }

            if (viewModel.EntityId != Guid.Empty && !string.IsNullOrEmpty(viewModel.EntityType))
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

            ReminderFormViewModel? viewModel = await _reminderService.GetFormAsync(id.Value);
            return viewModel == null ? NotFound() : View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, ReminderFormViewModel viewModel)
        {
            if (id != viewModel.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    OperationResult result = await _reminderService.UpdateAsync(id, viewModel);
                    if (result.Success)
                    {
                        return RedirectToEntity(result.RedirectId, result.RedirectType);
                    }
                    if (result.ErrorMessage == "Reminder not found.")
                    {
                        return NotFound();
                    }
                }
                catch (Exception)
                {
                    throw;
                }
            }

            if (viewModel.EntityId != Guid.Empty && !string.IsNullOrEmpty(viewModel.EntityType))
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

            ReminderDto? dto = await _reminderService.GetDtoAsync(id.Value);
            if (dto == null)
            {
                return NotFound();
            }

            ReminderDeleteViewModel viewModel = new()
            {
                Id = dto.Id,
                Title = dto.Title,
                Description = dto.Description,
                DueDate = dto.DueDate,
                IsCompleted = dto.IsCompleted,
                EntityId = dto.EntityId,
                EntityType = dto.EntityType,
                RemindMe = dto.RemindMe,
                ReminderSent = dto.ReminderSent,
                EventFrequency = dto.EventFrequency,
                EntityName = await _entityService.GetEntityNameAsync(dto.EntityType, dto.EntityId)
            };
            return View(viewModel);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            OperationResult result = await _reminderService.DeleteAsync(id);
            return result.Success ? RedirectToEntity(result.RedirectId, result.RedirectType) : RedirectToAction("Index", "Home");
        }
    }
}
