using Microsoft.AspNetCore.Mvc;
using Rvnx.CRM.Core.Constants;
using Rvnx.CRM.Core.DTOs.Dates;
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
            if (entityId == Guid.Empty || await IsPartialContactAsync(entityId))
            {
                return NotFound();
            }

            ReminderDto dto = new Reminder
            {
                ContactId = entityId,
                DueDate = DateTime.Now.AddDays(1),
                EventFrequency = TimeSpan.FromDays(365) // Default
            }.ToDto();

            ReminderFormViewModel viewModel = new()
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
            if (await IsPartialContactAsync(viewModel.EntityId)) return NotFound();

            if (ModelState.IsValid)
            {
                Reminder reminder = new()
                {
                    Id = Guid.NewGuid(),
                    Title = viewModel.Title,
                    Description = viewModel.Description,
                    DueDate = viewModel.DueDate,
                    IsCompleted = viewModel.IsCompleted,
                    ContactId = viewModel.EntityId,
                    RemindMe = viewModel.RemindMe,
                    EventFrequency = viewModel.EventFrequency
                };

                await Repository.AddAsync(reminder);
                await Repository.SaveChangesAsync();
                return RedirectToEntity(reminder.ContactId ?? Guid.Empty, EntityTypes.Person);
            }

            if (viewModel.EntityId != Guid.Empty && !string.IsNullOrEmpty(viewModel.EntityType))
            {
                viewModel.EntityName = await GetEntityName(viewModel.EntityId, viewModel.EntityType);
            }
            return View(viewModel);
        }

        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            Reminder? reminder = await Repository.GetByIdAsync<Reminder>(id.Value);
            if (reminder == null || await IsPartialContactAsync(reminder.ContactId ?? Guid.Empty))
            {
                return NotFound();
            }

            ReminderDto dto = reminder.ToDto();
            ReminderFormViewModel viewModel = new()
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
            if (id != viewModel.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    Reminder? reminder = await Repository.GetByIdAsync<Reminder>(id);
                    if (reminder == null || await IsPartialContactAsync(reminder.ContactId ?? Guid.Empty))
                    {
                        return NotFound();
                    }

                    // UpdateEntity expects ReminderDto. Since viewModel inherits ReminderDto, this works.
                    reminder.UpdateEntity(viewModel);

                    await Repository.UpdateAsync(reminder);
                    await Repository.SaveChangesAsync();
                }
                catch (Exception)
                {
                    if (!await Repository.ExistsAsync<Reminder>(viewModel.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
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
            if (id == null)
            {
                return NotFound();
            }

            Reminder? reminder = await Repository.GetByIdAsync<Reminder>(id.Value);
            if (reminder == null || await IsPartialContactAsync(reminder.ContactId ?? Guid.Empty))
            {
                return NotFound();
            }

            ReminderDto dto = reminder.ToDto();
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
                EntityName = await GetEntityName(dto.EntityId, dto.EntityType)
            };
            return View(viewModel);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            Reminder? reminder = await Repository.GetByIdAsync<Reminder>(id);
            if (reminder != null)
            {
                Guid entityId = reminder.ContactId ?? Guid.Empty;
                string entityType = EntityTypes.Person;
                await Repository.DeleteAsync<Reminder>(id);
                await Repository.SaveChangesAsync();
                return RedirectToEntity(entityId, entityType);
            }
            return RedirectToAction("Index", "Home");
        }
    }
}
