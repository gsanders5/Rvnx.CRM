using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Rvnx.CRM.Core.Constants;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models.Business;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Web.Controllers.Base;

namespace Rvnx.CRM.Web.Controllers
{
    public class RelationshipsController : AuthorizedController
    {
        private readonly IRepository _repository;

        public RelationshipsController(IRepository repository)
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

        private IActionResult RedirectToEntity(Guid id, string type)
        {
            if (type == EntityTypes.Person)
            {
                return RedirectToAction("Details", "Contacts", new { id });
            }
            // Add other types here
            return RedirectToAction("Index", "Home");
        }

        // GET: Relationships/Create
        public async Task<IActionResult> Create(Guid entityId, string entityType)
        {
            if (entityId == Guid.Empty || string.IsNullOrEmpty(entityType)) return NotFound();

            ViewData["EntityName"] = await GetEntityName(entityId, entityType);
            ViewData["EntityId"] = entityId;
            ViewData["EntityType"] = entityType;

            if (entityType == EntityTypes.Person)
            {
                var all = await _repository.ListAsync<Contact>();
                var available = all.Where(p => p.Id != entityId).OrderBy(p => p.FullName).ToList();
                ViewData["RelatedEntityId"] = new SelectList(available, "Id", "FullName");
            }
            else if (entityType == EntityTypes.Company)
            {
                var all = await _repository.ListAsync<Employer>();
                var available = all.Where(c => c.Id != entityId).OrderBy(c => c.CompanyName).ToList();
                ViewData["RelatedEntityId"] = new SelectList(available, "Id", "CompanyName");
            }

            var types = await _repository.ListAsync<RelationshipType>(t => t.EntityType == entityType);
            var options = new List<SelectListItem>();
            foreach (var t in types)
            {
                options.Add(new SelectListItem { Value = $"{t.Id}_Fwd", Text = $"is {t.Name} of" });
                if (!t.IsSymmetric)
                {
                    options.Add(new SelectListItem { Value = $"{t.Id}_Rev", Text = $"is {t.OppositeName} of" });
                }
            }
            ViewData["RelationshipTypeSelection"] = new SelectList(options, "Value", "Text");

            return View(new Relationship { EntityId = entityId, EntityType = entityType });
        }

        // POST: Relationships/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("EntityId,RelatedEntityId,EntityType,Description,StartDate,EndDate")] Relationship relationship, string relationshipTypeSelection)
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
                    var direction = parts[1];
                    relationship.RelationshipTypeId = typeId;

                    if (direction == "Rev")
                    {
                        // Swap logic for generic entities
                        // A (EntityId) is Child of B (RelatedEntityId).
                        // Logic: A is Child -> B is Parent.
                        // Record: EntityId=B, RelatedEntityId=A, Type=Parent.
                        var temp = relationship.EntityId;
                        relationship.EntityId = relationship.RelatedEntityId;
                        relationship.RelatedEntityId = temp;
                    }

                    relationship.Id = Guid.NewGuid();
                    await _repository.AddAsync(relationship);
                    await _repository.SaveChangesAsync();

                    // If direction was Rev, we swapped EntityId.
                    // But we want to redirect back to the page user was on (the original EntityId).
                    // If swapped, original EntityId is now relationship.RelatedEntityId.
                    // If not swapped, original EntityId is relationship.EntityId.
                    var redirectId = direction == "Rev" ? relationship.RelatedEntityId : relationship.EntityId;

                    return RedirectToEntity(redirectId, relationship.EntityType);
                }
                else
                {
                    ModelState.AddModelError("RelationshipTypeSelection", "Invalid Relationship Type.");
                }
            }

            // Reload view data
            ViewData["EntityName"] = await GetEntityName(relationship.EntityId, relationship.EntityType);
            ViewData["EntityId"] = relationship.EntityId;
            ViewData["EntityType"] = relationship.EntityType;

            if (relationship.EntityType == EntityTypes.Person)
            {
                var all = await _repository.ListAsync<Contact>();
                var available = all.Where(p => p.Id != relationship.EntityId).OrderBy(p => p.FullName).ToList();
                ViewData["RelatedEntityId"] = new SelectList(available, "Id", "FullName", relationship.RelatedEntityId);
            }
            else if (relationship.EntityType == EntityTypes.Company)
            {
                var all = await _repository.ListAsync<Employer>();
                var available = all.Where(c => c.Id != relationship.EntityId).OrderBy(c => c.CompanyName).ToList();
                ViewData["RelatedEntityId"] = new SelectList(available, "Id", "CompanyName", relationship.RelatedEntityId);
            }

            var types = await _repository.ListAsync<RelationshipType>(t => t.EntityType == relationship.EntityType);
            var options = new List<SelectListItem>();
            foreach (var t in types)
            {
                options.Add(new SelectListItem { Value = $"{t.Id}_Fwd", Text = $"is {t.Name} of" });
                if (!t.IsSymmetric)
                {
                    options.Add(new SelectListItem { Value = $"{t.Id}_Rev", Text = $"is {t.OppositeName} of" });
                }
            }
            ViewData["RelationshipTypeSelection"] = new SelectList(options, "Value", "Text", relationshipTypeSelection);

            return View(relationship);
        }

        // GET: Relationships/Edit/5
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null) return NotFound();

            var relationship = await _repository.GetByIdAsync<Relationship>(id.Value);
            if (relationship == null) return NotFound();

            // Fetch RelationshipType manually as GetByIdAsync doesn't include
            var type = await _repository.GetByIdAsync<RelationshipType>(relationship.RelationshipTypeId);
            relationship.RelationshipType = type;

            ViewData["EntityName"] = await GetEntityName(relationship.EntityId, relationship.EntityType);
            ViewData["EntityId"] = relationship.EntityId;
            ViewData["EntityType"] = relationship.EntityType;

            if (relationship.EntityType == EntityTypes.Person)
            {
                var all = await _repository.ListAsync<Contact>();
                var available = all.Where(p => p.Id != relationship.EntityId).OrderBy(p => p.FullName).ToList();
                ViewData["RelatedEntityId"] = new SelectList(available, "Id", "FullName", relationship.RelatedEntityId);
            }
            else if (relationship.EntityType == EntityTypes.Company)
            {
                var all = await _repository.ListAsync<Employer>();
                var available = all.Where(c => c.Id != relationship.EntityId).OrderBy(c => c.CompanyName).ToList();
                ViewData["RelatedEntityId"] = new SelectList(available, "Id", "CompanyName", relationship.RelatedEntityId);
            }

            var types = await _repository.ListAsync<RelationshipType>(t => t.EntityType == relationship.EntityType);
            var options = new List<SelectListItem>();
            foreach (var t in types)
            {
                options.Add(new SelectListItem { Value = $"{t.Id}_Fwd", Text = $"is {t.Name} of" });
                if (!t.IsSymmetric)
                {
                    options.Add(new SelectListItem { Value = $"{t.Id}_Rev", Text = $"is {t.OppositeName} of" });
                }
            }

            var currentSelection = $"{relationship.RelationshipTypeId}_Fwd";
            // Note: If relationship was created via "Rev", it's stored as "Fwd" (swapped).
            // So we always edit as "Fwd" relative to the stored EntityId.
            // But if the user is editing from the perspective of the "RelatedEntity" (reverse view),
            // we might want to show it as Rev.
            // But here we are editing the relationship record itself.
            // Let's keep it simple: always Fwd relative to EntityId.

            ViewData["RelationshipTypeSelection"] = new SelectList(options, "Value", "Text", currentSelection);

            return View(relationship);
        }

        // POST: Relationships/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, [Bind("Id,EntityId,RelatedEntityId,EntityType,RelationshipTypeId,Description,StartDate,EndDate")] Relationship relationship, string relationshipTypeSelection)
        {
            if (id != relationship.Id) return NotFound();

            if (string.IsNullOrEmpty(relationshipTypeSelection))
            {
                ModelState.AddModelError("RelationshipTypeSelection", "Relationship Type is required.");
            }

            if (ModelState.IsValid)
            {
                var parts = relationshipTypeSelection.Split('_');
                if (parts.Length == 2 && Guid.TryParse(parts[0], out var typeId))
                {
                    var direction = parts[1];
                    relationship.RelationshipTypeId = typeId;

                    if (direction == "Rev")
                    {
                        var temp = relationship.EntityId;
                        relationship.EntityId = relationship.RelatedEntityId;
                        relationship.RelatedEntityId = temp;
                    }

                    await _repository.UpdateAsync(relationship);
                    await _repository.SaveChangesAsync();

                    var redirectId = direction == "Rev" ? relationship.RelatedEntityId : relationship.EntityId;
                    return RedirectToEntity(redirectId, relationship.EntityType);
                }
                else
                {
                    ModelState.AddModelError("RelationshipTypeSelection", "Invalid Relationship Type.");
                }
            }
            // Reload data... (omitted for brevity, assume similar to Create)
            return View(relationship);
        }

        // GET: Relationships/Delete/5
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null) return NotFound();
            var relationship = await _repository.GetByIdAsync<Relationship>(id.Value);
            if (relationship == null) return NotFound();
            // Manually load types/names for display
            relationship.RelationshipType = await _repository.GetByIdAsync<RelationshipType>(relationship.RelationshipTypeId);
            relationship.Person = await _repository.GetByIdAsync<Contact>(relationship.EntityId); // Assumption
            relationship.RelatedPerson = await _repository.GetByIdAsync<Contact>(relationship.RelatedEntityId); // Assumption
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
                var entityId = relationship.EntityId;
                var entityType = relationship.EntityType;
                await _repository.DeleteAsync<Relationship>(id);
                await _repository.SaveChangesAsync();
                return RedirectToEntity(entityId, entityType);
            }
            return RedirectToAction("Index", "Home");
        }
    }
}
