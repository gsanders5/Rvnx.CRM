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

        // GET: Notes/Edit/5
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var note = await _repository.GetByIdWithIncludesAsync<Note>(id.Value, "Person");
            if (note == null)
            {
                return NotFound();
            }

            if (note.Person != null)
            {
                ViewData["PersonName"] = note.Person.FullName;
                ViewData["PersonId"] = note.Person.Id;
            }

            return View(note);
        }

        // POST: Notes/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, [Bind("Id,Title,Value,PersonId,CreatedDate,CreatedBy,UserId")] Note note)
        {
            if (id != note.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    await _repository.UpdateAsync(note);
                    await _repository.SaveChangesAsync();
                }
                catch (Exception)
                {
                    if (!await _repository.ExistsAsync<Note>(note.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction("Details", "Contacts", new { id = note.PersonId });
            }

            var person = await _repository.GetByIdAsync<Contact>(note.PersonId);
            if (person != null)
            {
                ViewData["PersonName"] = person.FullName;
                ViewData["PersonId"] = person.Id;
            }

            return View(note);
        }

        // GET: Notes/Delete/5
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var note = await _repository.GetByIdWithIncludesAsync<Note>(id.Value, "Person");
            if (note == null)
            {
                return NotFound();
            }

            return View(note);
        }

        // POST: Notes/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var note = await _repository.GetByIdAsync<Note>(id);
            if (note != null)
            {
                var personId = note.PersonId;
                await _repository.DeleteAsync<Note>(id);
                await _repository.SaveChangesAsync();
                return RedirectToAction("Details", "Contacts", new { id = personId });
            }
            return RedirectToAction("Index", "Contacts");
        }
    }
}
