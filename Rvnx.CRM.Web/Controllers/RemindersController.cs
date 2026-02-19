using Microsoft.AspNetCore.Mvc;
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

            ViewData["EntityName"] = await GetEntityName(entityId, entityType);
            ViewData["EntityId"] = entityId;
            ViewData["EntityType"] = entityType;

            return View(new Reminder
            {
                EntityId = entityId,
                EntityType = entityType,
                DueDate = DateTime.Now.AddDays(1),
                EventFrequency = TimeSpan.FromDays(365) // Default
            }.ToDto());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Title,Description,DueDate,IsCompleted,EntityId,EntityType,RemindMe,EventFrequency")] Core.DTOs.Common.ReminderDto reminderDto)
        {
            if (ModelState.IsValid)
            {
                Reminder reminder = new()
                {
                    Id = Guid.NewGuid(),
                    Title = reminderDto.Title,
                    Description = reminderDto.Description,
                    DueDate = reminderDto.DueDate,
                    IsCompleted = reminderDto.IsCompleted,
                    EntityId = reminderDto.EntityId,
                    EntityType = reminderDto.EntityType,
                    RemindMe = reminderDto.RemindMe,
                    EventFrequency = reminderDto.EventFrequency
                };

                await _repository.AddAsync(reminder);
                await _repository.SaveChangesAsync();
                return RedirectToEntity(reminder.EntityId, reminder.EntityType);
            }

            if (reminderDto.EntityId != Guid.Empty && !string.IsNullOrEmpty(reminderDto.EntityType))
            {
                ViewData["EntityName"] = await GetEntityName(reminderDto.EntityId, reminderDto.EntityType);
                ViewData["EntityId"] = reminderDto.EntityId;
                ViewData["EntityType"] = reminderDto.EntityType;
            }
            return View(reminderDto);
        }

        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null) return NotFound();

            Reminder? reminder = await _repository.GetByIdAsync<Reminder>(id.Value);
            if (reminder == null) return NotFound();

            if (reminder.EntityId != Guid.Empty && !string.IsNullOrEmpty(reminder.EntityType))
            {
                ViewData["EntityName"] = await GetEntityName(reminder.EntityId, reminder.EntityType);
            }

            return View(reminder.ToDto());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, [Bind("Id,Title,Description,DueDate,IsCompleted,EntityId,EntityType,RemindMe,EventFrequency")] Core.DTOs.Common.ReminderDto reminderDto)
        {
            if (id != reminderDto.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    Reminder? reminder = await _repository.GetByIdAsync<Reminder>(id);
                    if (reminder == null) return NotFound();

                    reminder.Title = reminderDto.Title;
                    reminder.Description = reminderDto.Description;
                    reminder.DueDate = reminderDto.DueDate;
                    reminder.IsCompleted = reminderDto.IsCompleted;
                    reminder.RemindMe = reminderDto.RemindMe;
                    reminder.EventFrequency = reminderDto.EventFrequency;

                    await _repository.UpdateAsync(reminder);
                    await _repository.SaveChangesAsync();
                }
                catch (Exception)
                {
                    if (!await _repository.ExistsAsync<Reminder>(reminderDto.Id)) return NotFound();
                    else throw;
                }
                return RedirectToEntity(reminderDto.EntityId, reminderDto.EntityType);
            }

            if (reminderDto.EntityId != Guid.Empty && !string.IsNullOrEmpty(reminderDto.EntityType))
            {
                ViewData["EntityName"] = await GetEntityName(reminderDto.EntityId, reminderDto.EntityType);
            }
            return View(reminderDto);
        }

        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null) return NotFound();
            Reminder? reminder = await _repository.GetByIdAsync<Reminder>(id.Value);
            if (reminder == null) return NotFound();

            if (reminder.EntityId != Guid.Empty && !string.IsNullOrEmpty(reminder.EntityType))
            {
                ViewData["EntityName"] = await GetEntityName(reminder.EntityId, reminder.EntityType);
            }
            return View(reminder.ToDto());
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
