using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models.Base;
using Rvnx.CRM.Core.Models.Contact;

namespace Rvnx.CRM.Web.Controllers
{
    public class RelationshipsController : Controller
    {
        private readonly IRepository _repository;

        public RelationshipsController(IRepository repository)
        {
            _repository = repository;
        }

        // GET: Relationships/Create?personId=...
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

            // Get potential related people (exclude current person)
            var allPeople = await _repository.ListAsync<Contact>();
            var availablePeople = allPeople.Where(p => p.Id != personId).ToList();

            ViewData["PersonName"] = person.FullName;
            ViewData["RelatedPersonId"] = new SelectList(availablePeople, "Id", "FullName");

            // Standard relationship types
            var types = new List<string> { "Partner", "Spouse", "Child", "Parent", "Sibling", "Friend", "Colleague", "Manager", "Direct Report", "Other" };
            ViewData["Type"] = new SelectList(types);

            return View(new Relationship { PersonId = person.Id });
        }

        // POST: Relationships/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("PersonId,RelatedPersonId,Type,Description,StartDate,EndDate,UserId")] Relationship relationship)
        {
            if (ModelState.IsValid)
            {
                relationship.Id = Guid.NewGuid();
                await _repository.AddAsync(relationship);
                await _repository.SaveChangesAsync();
                return RedirectToAction("Details", "Contacts", new { id = relationship.PersonId });
            }

            // Reload data if validation fails
            var person = await _repository.GetByIdAsync<Contact>(relationship.PersonId);
            if (person != null)
            {
                ViewData["PersonName"] = person.FullName;
            }

            var allPeople = await _repository.ListAsync<Contact>();
            var availablePeople = allPeople.Where(p => p.Id != relationship.PersonId).ToList();
            ViewData["RelatedPersonId"] = new SelectList(availablePeople, "Id", "FullName", relationship.RelatedPersonId);

            var types = new List<string> { "Partner", "Spouse", "Child", "Parent", "Sibling", "Friend", "Colleague", "Manager", "Direct Report", "Other" };
            ViewData["Type"] = new SelectList(types, relationship.Type);

            return View(relationship);
        }
    }
}
