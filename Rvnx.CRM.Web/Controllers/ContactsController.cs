using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Core.Models.Dates;
using Rvnx.CRM.Core.Models.Base;

namespace Rvnx.CRM.Web.Controllers
{
    public class ContactsController : Controller
    {
        private readonly IRepository _repository;
        private readonly ILogger<ContactsController> _logger;

        public ContactsController(IRepository repository, ILogger<ContactsController> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        // GET: Contacts
        public async Task<IActionResult> Index()
        {
            var contacts = await _repository.ListAsync<Contact>();
            return View(contacts);
        }

        // GET: Contacts/Details/5
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var contact = await _repository.GetByIdWithIncludesAsync<Contact>(
                id.Value,
                "Notes",
                "Relationships.RelatedPerson",
                "Relationships.RelationshipType",
                "RelatedTo.Person",
                "RelatedTo.RelationshipType",
                "Reminders",
                "Employers",
                "ImportantDates"
            );

            if (contact == null)
            {
                return NotFound();
            }

            return View(contact);
        }

        // GET: Contacts/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Contacts/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("FirstName,LastName,Email,Phone,JobTitle,Company,Birthday,UserId")] Contact contact)
        {
            if (ModelState.IsValid)
            {
                contact.Id = Guid.NewGuid();
                await _repository.AddAsync(contact);
                await _repository.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(contact);
        }

        // GET: Contacts/Edit/5
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var contact = await _repository.GetByIdAsync<Contact>(id.Value);
            if (contact == null)
            {
                return NotFound();
            }
            return View(contact);
        }

        // POST: Contacts/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, [Bind("Id,FirstName,LastName,Email,Phone,JobTitle,Company,Birthday")] Contact contact)
        {
            if (id != contact.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var existingContact = await _repository.GetByIdAsync<Contact>(id);
                    if (existingContact == null)
                    {
                        return NotFound();
                    }

                    existingContact.FirstName = contact.FirstName;
                    existingContact.LastName = contact.LastName;
                    existingContact.Email = contact.Email;
                    existingContact.Phone = contact.Phone;
                    existingContact.JobTitle = contact.JobTitle;
                    existingContact.Company = contact.Company;
                    existingContact.Birthday = contact.Birthday;

                    await _repository.UpdateAsync(existingContact);
                    await _repository.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await ContactExists(contact.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(contact);
        }

        // GET: Contacts/Delete/5
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var contact = await _repository.GetByIdAsync<Contact>(id.Value);
            if (contact == null)
            {
                return NotFound();
            }

            return View(contact);
        }

        // POST: Contacts/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var contact = await _repository.GetByIdWithIncludesAsync<Contact>(id, "Relationships", "RelatedTo");

            if (contact != null)
            {
                if (contact.Relationships.Any())
                {
                    await _repository.DeleteRangeAsync(contact.Relationships);
                }
                if (contact.RelatedTo.Any())
                {
                    await _repository.DeleteRangeAsync(contact.RelatedTo);
                }

                await _repository.DeleteAsync<Contact>(id);
                await _repository.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        private async Task<bool> ContactExists(Guid id)
        {
            return await _repository.ExistsAsync<Contact>(id);
        }
    }
}
