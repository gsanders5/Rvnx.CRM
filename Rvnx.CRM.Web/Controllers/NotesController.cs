using Microsoft.AspNetCore.Mvc;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models.Base;
using Rvnx.CRM.Core.Models.Contact;

namespace Rvnx.CRM.Web.Controllers
{
    public class NotesController : Controller
    {
        private readonly IRepository _repository;

        public NotesController(IRepository repository)
        {
            _repository = repository;
        }

        // GET: Notes/Create?personId=...
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

            return View(new Note { PersonId = person.Id });
        }

        // POST: Notes/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Title,Value,PersonId,UserId")] Note note)
        {
            if (ModelState.IsValid)
            {
                note.Id = Guid.NewGuid();
                await _repository.AddAsync(note);
                await _repository.SaveChangesAsync();
                return RedirectToAction("Details", "Contacts", new { id = note.PersonId });
            }

            // Reload person info if validation fails
            var person = await _repository.GetByIdAsync<Contact>(note.PersonId);
            if (person != null)
            {
                ViewData["PersonName"] = person.FullName;
                ViewData["PersonId"] = person.Id;
            }

            return View(note);
        }
    }
}
