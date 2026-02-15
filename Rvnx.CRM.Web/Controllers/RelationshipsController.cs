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
            var availablePeople = allPeople.Where(p => p.Id != personId).OrderBy(p => p.FullName).ToList();

            ViewData["PersonName"] = person.FullName;
            ViewData["RelatedPersonId"] = new SelectList(availablePeople, "Id", "FullName");

            // Relationship Types
            var types = await _repository.ListAsync<RelationshipType>();
            var options = new List<SelectListItem>();
            foreach (var t in types)
            {
                options.Add(new SelectListItem { Value = $"{t.Id}_Fwd", Text = t.Name });
                if (!t.IsSymmetric)
                {
                    options.Add(new SelectListItem { Value = $"{t.Id}_Rev", Text = t.OppositeName });
                }
            }
            ViewData["RelationshipTypeSelection"] = new SelectList(options, "Value", "Text");

            return View(new Relationship { PersonId = person.Id });
        }

        // POST: Relationships/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("PersonId,RelatedPersonId,Description,StartDate,EndDate,UserId")] Relationship relationship, string relationshipTypeSelection)
        {
            if (string.IsNullOrEmpty(relationshipTypeSelection))
            {
                ModelState.AddModelError("RelationshipTypeSelection", "Relationship Type is required.");
            }

            if (ModelState.IsValid)
            {
                var parts = relationshipTypeSelection.Split('_');
                if (parts.Length == 2 && Guid.TryParse(parts[0], out var typeId))
                {
                    var direction = parts[1]; // Fwd or Rev
                    relationship.RelationshipTypeId = typeId;

                    // Capture original person ID for redirection
                    var originalPersonId = relationship.PersonId;

                    if (direction == "Rev")
                    {
                        // Swap so DB record matches the "Forward" definition
                        // Example: User says "A is Child of B". (Selected "Child", which is Rev of Parent).
                        // Logic: A is Child -> B is Parent.
                        // Record: Person=B (Parent), Related=A (Child), Type=Parent.
                        var temp = relationship.PersonId;
                        relationship.PersonId = relationship.RelatedPersonId;
                        relationship.RelatedPersonId = temp;
                    }

                    relationship.Id = Guid.NewGuid();
                    await _repository.AddAsync(relationship);
                    await _repository.SaveChangesAsync();

                    return RedirectToAction("Details", "Contacts", new { id = originalPersonId });
                }
                else
                {
                     ModelState.AddModelError("RelationshipTypeSelection", "Invalid Relationship Type.");
                }
            }

            // Reload data if validation fails
            var person = await _repository.GetByIdAsync<Contact>(relationship.PersonId); // Note: PersonId might be swapped if failed after swap? No, struct is ref type but passed by value reference?
            // If validation failed BEFORE swap, PersonId is correct.
            // If validation failed AFTER swap (e.g. SaveChanges error), PersonId is swapped.
            // But we only swap if ModelState.IsValid is true initially.
            // So if we fall through here, it's likely pre-swap validation failure OR catch block (not here).

            // To be safe, rely on the ID passed in form?
            // Actually, if we are here, we need to reload purely based on initial state.
            // But `relationship` object is modified if we swapped.
            // Let's re-read PersonId from the original request?
            // Or just assume if we are here, we haven't swapped yet because `ModelState.IsValid` check covers most cases.
            // Exception: logic inside the IsValid block fails? No, we return Redirect.
            // So we are safe.

            if (person != null)
            {
                ViewData["PersonName"] = person.FullName;
            }

            var allPeople = await _repository.ListAsync<Contact>();
            // We need the Original Person ID to exclude.
            // If we haven't swapped, it's relationship.PersonId.
            var availablePeople = allPeople.Where(p => p.Id != relationship.PersonId).OrderBy(p => p.FullName).ToList();
            ViewData["RelatedPersonId"] = new SelectList(availablePeople, "Id", "FullName", relationship.RelatedPersonId);

            var types = await _repository.ListAsync<RelationshipType>();
            var options = new List<SelectListItem>();
            foreach (var t in types)
            {
                options.Add(new SelectListItem { Value = $"{t.Id}_Fwd", Text = t.Name });
                if (!t.IsSymmetric)
                {
                    options.Add(new SelectListItem { Value = $"{t.Id}_Rev", Text = t.OppositeName });
                }
            }
            ViewData["RelationshipTypeSelection"] = new SelectList(options, "Value", "Text", relationshipTypeSelection);

            return View(relationship);
        }

        // GET: Relationships/Edit/5
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var relationship = await _repository.GetByIdWithIncludesAsync<Relationship>(id.Value, "Person", "RelatedPerson", "RelationshipType");
            if (relationship == null)
            {
                return NotFound();
            }

            ViewData["PersonName"] = relationship.Person?.FullName;

            var allPeople = await _repository.ListAsync<Contact>();
            var availablePeople = allPeople.Where(p => p.Id != relationship.PersonId).OrderBy(p => p.FullName).ToList();

            ViewData["RelatedPersonId"] = new SelectList(availablePeople, "Id", "FullName", relationship.RelatedPersonId);

            var types = await _repository.ListAsync<RelationshipType>();
            var options = new List<SelectListItem>();
            foreach (var t in types)
            {
                options.Add(new SelectListItem { Value = $"{t.Id}_Fwd", Text = t.Name });
                if (!t.IsSymmetric)
                {
                    options.Add(new SelectListItem { Value = $"{t.Id}_Rev", Text = t.OppositeName });
                }
            }

            var currentSelection = $"{relationship.RelationshipTypeId}_Fwd";
            ViewData["RelationshipTypeSelection"] = new SelectList(options, "Value", "Text", currentSelection);

            return View(relationship);
        }

        // POST: Relationships/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, [Bind("Id,PersonId,RelatedPersonId,RelationshipTypeId,Description,StartDate,EndDate")] Relationship relationship, string relationshipTypeSelection)
        {
            if (id != relationship.Id)
            {
                return NotFound();
            }

            if (string.IsNullOrEmpty(relationshipTypeSelection))
            {
                ModelState.AddModelError("RelationshipTypeSelection", "Relationship Type is required.");
            }

            if (ModelState.IsValid)
            {
                var parts = relationshipTypeSelection.Split('_');
                if (parts.Length == 2 && Guid.TryParse(parts[0], out var typeId))
                {
                    var direction = parts[1]; // Fwd or Rev
                    relationship.RelationshipTypeId = typeId;

                    if (direction == "Rev")
                    {
                        var temp = relationship.PersonId;
                        relationship.PersonId = relationship.RelatedPersonId;
                        relationship.RelatedPersonId = temp;
                    }

                    await _repository.UpdateAsync(relationship);
                    await _repository.SaveChangesAsync();

                    return RedirectToAction("Details", "Contacts", new { id = relationship.PersonId });
                }
                else
                {
                    ModelState.AddModelError("RelationshipTypeSelection", "Invalid Relationship Type.");
                }
            }

            var allPeople = await _repository.ListAsync<Contact>();
            var availablePeople = allPeople.Where(p => p.Id != relationship.PersonId).OrderBy(p => p.FullName).ToList();
            ViewData["RelatedPersonId"] = new SelectList(availablePeople, "Id", "FullName", relationship.RelatedPersonId);

            var types = await _repository.ListAsync<RelationshipType>();
            var options = new List<SelectListItem>();
            foreach (var t in types)
            {
                options.Add(new SelectListItem { Value = $"{t.Id}_Fwd", Text = t.Name });
                if (!t.IsSymmetric)
                {
                    options.Add(new SelectListItem { Value = $"{t.Id}_Rev", Text = t.OppositeName });
                }
            }
            ViewData["RelationshipTypeSelection"] = new SelectList(options, "Value", "Text", relationshipTypeSelection);

            return View(relationship);
        }

        // GET: Relationships/Delete/5
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var relationship = await _repository.GetByIdWithIncludesAsync<Relationship>(id.Value, "Person", "RelatedPerson", "RelationshipType");
            if (relationship == null)
            {
                return NotFound();
            }

            return View(relationship);
        }

        // POST: Relationships/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var relationship = await _repository.GetByIdAsync<Relationship>(id);
            if (relationship != null)
            {
                var personId = relationship.PersonId;
                await _repository.DeleteAsync<Relationship>(id);
                await _repository.SaveChangesAsync();
                return RedirectToAction("Details", "Contacts", new { id = personId });
            }
            return RedirectToAction("Index", "Contacts");
        }
    }
}
