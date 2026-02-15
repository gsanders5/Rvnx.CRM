using Microsoft.AspNetCore.Mvc;
using Rvnx.CRM.Core.Constants;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models.Business;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Core.Models.Dates;
using Rvnx.CRM.Web.Controllers.Base;

namespace Rvnx.CRM.Web.Controllers
{
    public class RemindersController : BaseAuthorizedController
    {
        private readonly IRepository _repository;

        public RemindersController(IRepository repository)
        {
            _repository = repository;
        }

        private async Task<string> GetEntityName(Guid id, string type)
        {
            if (type == EntityTypes.Person)
            {
                var p = await _repository.GetByIdAsync<Contact>(id);
                return p?.FullName ?? "Unknown Person";
            }
            else if (type == EntityTypes.Company)
            {
                var c = await _repository.GetByIdAsync<Employer>(id);
                return c?.CompanyName ?? "Unknown Company";
            }
            return "Unknown Entity";
        }

        private IActionResult RedirectToEntity(Guid id, string? type)
        {
            if (id == Guid.Empty || string.IsNullOrEmpty(type)) return RedirectToAction("Index", "Home");

            if (type == EntityTypes.Person)
            {
                return RedirectToAction("Details", "Contacts", new { id });
            }
            // Add other types here
            return RedirectToAction("Index", "Home");
        }

        // GET: Reminders/Create
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
                DueDate = DateTime.Now.AddDays(1)
            });
        }

        // POST: Reminders/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Title,Description,DueDate,IsCompleted,EntityId,EntityType")] Reminder reminder)
        {
            if (ModelState.IsValid)
            {
                reminder.Id = Guid.NewGuid();
                await _repository.AddAsync(reminder);
                await _repository.SaveChangesAsync();
                return RedirectToEntity(reminder.EntityId, reminder.EntityType);
            }

            if (reminder.EntityId != Guid.Empty && !string.IsNullOrEmpty(reminder.EntityType))
            {
                ViewData["EntityName"] = await GetEntityName(reminder.EntityId, reminder.EntityType);
                ViewData["EntityId"] = reminder.EntityId;
                ViewData["EntityType"] = reminder.EntityType;
            }
            return View(reminder);
        }

        // GET: Reminders/Edit/5
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null) return NotFound();

            var reminder = await _repository.GetByIdAsync<Reminder>(id.Value);
            if (reminder == null) return NotFound();

            if (reminder.EntityId != Guid.Empty && !string.IsNullOrEmpty(reminder.EntityType))
            {
                ViewData["EntityName"] = await GetEntityName(reminder.EntityId, reminder.EntityType);
            }

            return View(reminder);
        }

        // POST: Reminders/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, [Bind("Id,Title,Description,DueDate,IsCompleted,EntityId,EntityType,CreatedDate,CreatedBy")] Reminder reminder)
        {
            if (id != reminder.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    await _repository.UpdateAsync(reminder);
                    await _repository.SaveChangesAsync();
                }
                catch (Exception)
                {
                    if (!await _repository.ExistsAsync<Reminder>(reminder.Id)) return NotFound();
                    else throw;
                }
                return RedirectToEntity(reminder.EntityId, reminder.EntityType);
            }

            if (reminder.EntityId != Guid.Empty && !string.IsNullOrEmpty(reminder.EntityType))
            {
                ViewData["EntityName"] = await GetEntityName(reminder.EntityId, reminder.EntityType);
            }
            return View(reminder);
        }

        // GET: Reminders/Delete/5
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null) return NotFound();
            var reminder = await _repository.GetByIdAsync<Reminder>(id.Value);
            if (reminder == null) return NotFound();

            if (reminder.EntityId != Guid.Empty && !string.IsNullOrEmpty(reminder.EntityType))
            {
                ViewData["EntityName"] = await GetEntityName(reminder.EntityId, reminder.EntityType);
            }
            return View(reminder);
        }

        // POST: Reminders/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var reminder = await _repository.GetByIdAsync<Reminder>(id);
            if (reminder != null)
            {
                var entityId = reminder.EntityId;
                var entityType = reminder.EntityType;
                await _repository.DeleteAsync<Reminder>(id);
                await _repository.SaveChangesAsync();
                return RedirectToEntity(entityId, entityType);
            }
            return RedirectToAction("Index", "Home");
        }
    }
}
