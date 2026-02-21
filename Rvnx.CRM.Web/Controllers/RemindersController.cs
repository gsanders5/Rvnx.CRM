using Microsoft.AspNetCore.Mvc;
using Rvnx.CRM.Core.DTOs.Common;
using Rvnx.CRM.Core.Extensions;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models.Dates;
using Rvnx.CRM.Web.Controllers.Base;

namespace Rvnx.CRM.Web.Controllers
{
    public class RemindersController(IRepository repository) : RepositoryController(repository)
    {
        public async Task<IActionResult> Create(Guid entityId, string entityType)
        {
            if (entityId == Guid.Empty || string.IsNullOrEmpty(entityType)) return NotFound();

            var dto = new Reminder
            {
                EntityId = entityId,
                EntityType = entityType,
                DueDate = DateTime.Now.AddDays(1),
                EventFrequency = TimeSpan.FromDays(365) // Default
            }.ToDto();

            var viewModel = new ReminderFormViewModel
            {
                // Copy properties from dto
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

                EntityName = await GetEntityName(entityId, entityType)
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ReminderFormViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                Reminder reminder = new()
                {
                    Id = Guid.NewGuid(),
                    Title = viewModel.Title,
                    Description = viewModel.Description,
                    DueDate = viewModel.DueDate,
                    IsCompleted = viewModel.IsCompleted,
                    EntityId = viewModel.EntityId,
                    EntityType = viewModel.EntityType,
                    RemindMe = viewModel.RemindMe,
                    EventFrequency = viewModel.EventFrequency
                };

                await _repository.AddAsync(reminder);
                await _repository.SaveChangesAsync();
                return RedirectToEntity(reminder.EntityId, reminder.EntityType);
            }

            if (viewModel.EntityId != Guid.Empty && !string.IsNullOrEmpty(viewModel.EntityType))
            {
                viewModel.EntityName = await GetEntityName(viewModel.EntityId, viewModel.EntityType);
            }
            return View(viewModel);
        }

        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null) return NotFound();

            Reminder? reminder = await _repository.GetByIdAsync<Reminder>(id.Value);
            if (reminder == null) return NotFound();

            var dto = reminder.ToDto();
            var viewModel = new ReminderFormViewModel
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
                EntityName = await GetEntityName(dto.EntityId, dto.EntityType)
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, ReminderFormViewModel viewModel)
        {
            if (id != viewModel.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    Reminder? reminder = await _repository.GetByIdAsync<Reminder>(id);
                    if (reminder == null) return NotFound();

                    // UpdateEntity expects ReminderDto. Since viewModel inherits ReminderDto, this works.
                    reminder.UpdateEntity(viewModel);

                    await _repository.UpdateAsync(reminder);
                    await _repository.SaveChangesAsync();
                }
                catch (Exception)
                {
                    if (!await _repository.ExistsAsync<Reminder>(viewModel.Id)) return NotFound();
                    else throw;
                }
                return RedirectToEntity(viewModel.EntityId, viewModel.EntityType);
            }

            if (viewModel.EntityId != Guid.Empty && !string.IsNullOrEmpty(viewModel.EntityType))
            {
                viewModel.EntityName = await GetEntityName(viewModel.EntityId, viewModel.EntityType);
            }
            return View(viewModel);
        }

        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null) return NotFound();
            Reminder? reminder = await _repository.GetByIdAsync<Reminder>(id.Value);
            if (reminder == null) return NotFound();

            var dto = reminder.ToDto();
            var viewModel = new ReminderDeleteViewModel
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
                EntityName = await GetEntityName(dto.EntityId, dto.EntityType)
            };
            return View(viewModel);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            Reminder? reminder = await _repository.GetByIdAsync<Reminder>(id);
            if (reminder != null)
            {
                Guid entityId = reminder.EntityId;
                string entityType = reminder.EntityType;
                await _repository.DeleteAsync<Reminder>(id);
                await _repository.SaveChangesAsync();
                return RedirectToEntity(entityId, entityType);
            }
            return RedirectToAction("Index", "Home");
        }
    }
}
