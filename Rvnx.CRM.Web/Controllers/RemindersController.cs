using Microsoft.AspNetCore.Mvc;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models.Base;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Core.Models.Dates;

namespace Rvnx.CRM.Web.Controllers
{
    public class RemindersController : Controller
    {
        private readonly IRepository _repository;

        public RemindersController(IRepository repository)
        {
            _repository = repository;
        }

        // GET: Reminders/Create?personId=...
        public async Task<IActionResult> Create(Guid? personId)
        {
            if (personId == null)
            {
                return NotFound();
            }

            var person = await _repository.GetByIdAsync<Contact>(personId.Value);
            if (person == null)
            {
                return NotFound();
            }

            ViewData["PersonName"] = person.FullName;
            ViewData["PersonId"] = person.Id;

            // Default to tomorrow
            var reminder = new Reminder
            {
                PersonId = person.Id,
                DueDate = DateTime.Now.AddDays(1)
            };

            return View(reminder);
        }

        // POST: Reminders/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Title,Description,DueDate,IsCompleted,PersonId,UserId")] Reminder reminder)
        {
            if (ModelState.IsValid)
            {
                reminder.Id = Guid.NewGuid();
                await _repository.AddAsync(reminder);
                await _repository.SaveChangesAsync();

                if (reminder.PersonId.HasValue)
                {
                    return RedirectToAction("Details", "Contacts", new { id = reminder.PersonId });
                }
                return RedirectToAction("Index", "Home"); // Should not happen in this flow
            }

            if (reminder.PersonId.HasValue)
            {
                var person = await _repository.GetByIdAsync<Contact>(reminder.PersonId.Value);
                if (person != null)
                {
                    ViewData["PersonName"] = person.FullName;
                    ViewData["PersonId"] = person.Id;
                }
            }

            return View(reminder);
        }

        // GET: Reminders/Edit/5
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var reminder = await _repository.GetByIdWithIncludesAsync<Reminder>(id.Value, "Person");
            if (reminder == null)
            {
                return NotFound();
            }

            if (reminder.Person != null)
            {
                ViewData["PersonName"] = reminder.Person.FullName;
                ViewData["PersonId"] = reminder.Person.Id;
            }

            return View(reminder);
        }

        // POST: Reminders/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, [Bind("Id,Title,Description,DueDate,IsCompleted,PersonId,UserId")] Reminder reminder)
        {
            if (id != reminder.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    await _repository.UpdateAsync(reminder);
                    await _repository.SaveChangesAsync();
                }
                catch (Exception)
                {
                    if (!await _repository.ExistsAsync<Reminder>(reminder.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }

                if (reminder.PersonId.HasValue)
                {
                    return RedirectToAction("Details", "Contacts", new { id = reminder.PersonId });
                }
                return RedirectToAction("Index", "Home");
            }

            if (reminder.PersonId.HasValue)
            {
                var person = await _repository.GetByIdAsync<Contact>(reminder.PersonId.Value);
                if (person != null)
                {
                    ViewData["PersonName"] = person.FullName;
                    ViewData["PersonId"] = person.Id;
                }
            }

            return View(reminder);
        }

        // GET: Reminders/Delete/5
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var reminder = await _repository.GetByIdWithIncludesAsync<Reminder>(id.Value, "Person");
            if (reminder == null)
            {
                return NotFound();
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
                var personId = reminder.PersonId;
                await _repository.DeleteAsync<Reminder>(id);
                await _repository.SaveChangesAsync();

                if (personId.HasValue)
                {
                    return RedirectToAction("Details", "Contacts", new { id = personId });
                }
                return RedirectToAction("Index", "Home");
            }
            return RedirectToAction("Index", "Contacts");
        }
    }
}
