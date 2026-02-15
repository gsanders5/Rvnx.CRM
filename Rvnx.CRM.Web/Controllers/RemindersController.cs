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
    }
}
