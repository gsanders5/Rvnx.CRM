using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Rvnx.CRM.Core.Constants;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models.Business;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Core.Services;
using Rvnx.CRM.Web.Controllers.Base;

namespace Rvnx.CRM.Web.Controllers
{
    public class RelationshipsController : RepositoryController
    {
        public RelationshipsController(IRepository repository) : base(repository)
        {
        }

        public async Task<IActionResult> Create(Guid entityId, string entityType)
        {
            if (entityId == Guid.Empty || string.IsNullOrEmpty(entityType)) return NotFound();

            ViewData["EntityName"] = await GetEntityName(entityId, entityType);
            ViewData["EntityId"] = entityId;
            ViewData["EntityType"] = entityType;

            await PopulateRelatedEntityDropdown(entityId, entityType);
            ViewData["RelationshipTypeSelection"] = GetRelationshipTypeOptions(entityType);

            return View(new Relationship { EntityId = entityId, EntityType = entityType });
        }

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
                string[] parts = relationshipTypeSelection.Split('_');
                if (parts.Length == 2 && Guid.TryParse(parts[0], out Guid typeId))
                {
                    string direction = parts[1];
                    relationship.RelationshipTypeId = typeId;

                    if (direction == "Rev")
                    {
                        Guid temp = relationship.EntityId;
                        relationship.EntityId = relationship.RelatedEntityId;
                        relationship.RelatedEntityId = temp;
                    }

                    relationship.Id = Guid.NewGuid();
                    await _repository.AddAsync(relationship);
                    await _repository.SaveChangesAsync();

                    Guid redirectId = direction == "Rev" ? relationship.RelatedEntityId : relationship.EntityId;

                    return RedirectToEntity(redirectId, relationship.EntityType);
                }
                else
                {
                    ModelState.AddModelError("RelationshipTypeSelection", "Invalid Relationship Type.");
                }
            }

            ViewData["EntityName"] = await GetEntityName(relationship.EntityId, relationship.EntityType);
            ViewData["EntityId"] = relationship.EntityId;
            ViewData["EntityType"] = relationship.EntityType;

            await PopulateRelatedEntityDropdown(relationship.EntityId, relationship.EntityType, relationship.RelatedEntityId);
            ViewData["RelationshipTypeSelection"] = GetRelationshipTypeOptions(relationship.EntityType, relationshipTypeSelection);

            return View(relationship);
        }

        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null) return NotFound();

            Relationship? relationship = await _repository.GetByIdAsync<Relationship>(id.Value);
            if (relationship == null) return NotFound();

            ViewData["EntityName"] = await GetEntityName(relationship.EntityId, relationship.EntityType);
            ViewData["EntityId"] = relationship.EntityId;
            ViewData["EntityType"] = relationship.EntityType;

            await PopulateRelatedEntityDropdown(relationship.EntityId, relationship.EntityType, relationship.RelatedEntityId);

            string currentSelection = $"{relationship.RelationshipTypeId}_Fwd";
            ViewData["RelationshipTypeSelection"] = GetRelationshipTypeOptions(relationship.EntityType, currentSelection);

            return View(relationship);
        }

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
                string[] parts = relationshipTypeSelection.Split('_');
                if (parts.Length == 2 && Guid.TryParse(parts[0], out Guid typeId))
                {
                    string direction = parts[1];
                    relationship.RelationshipTypeId = typeId;

                    if (direction == "Rev")
                    {
                        Guid temp = relationship.EntityId;
                        relationship.EntityId = relationship.RelatedEntityId;
                        relationship.RelatedEntityId = temp;
                    }

                    await _repository.UpdateAsync(relationship);
                    await _repository.SaveChangesAsync();

                    Guid redirectId = direction == "Rev" ? relationship.RelatedEntityId : relationship.EntityId;
                    return RedirectToEntity(redirectId, relationship.EntityType);
                }
                else
                {
                    ModelState.AddModelError("RelationshipTypeSelection", "Invalid Relationship Type.");
                }
            }

            ViewData["EntityName"] = await GetEntityName(relationship.EntityId, relationship.EntityType);
            ViewData["EntityId"] = relationship.EntityId;
            ViewData["EntityType"] = relationship.EntityType;

            await PopulateRelatedEntityDropdown(relationship.EntityId, relationship.EntityType, relationship.RelatedEntityId);
            ViewData["RelationshipTypeSelection"] = GetRelationshipTypeOptions(relationship.EntityType, relationshipTypeSelection);

            return View(relationship);
        }

        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null) return NotFound();
            Relationship? relationship = await _repository.GetByIdAsync<Relationship>(id.Value);
            if (relationship == null) return NotFound();

            // Populate Person/RelatedPerson so names show up
            relationship.Person = await _repository.GetByIdAsync<Contact>(relationship.EntityId);
            relationship.RelatedPerson = await _repository.GetByIdAsync<Contact>(relationship.RelatedEntityId);

            return View(relationship);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            Relationship? relationship = await _repository.GetByIdAsync<Relationship>(id);
            if (relationship != null)
            {
                Guid entityId = relationship.EntityId;
                string entityType = relationship.EntityType;
                await _repository.DeleteAsync<Relationship>(id);
                await _repository.SaveChangesAsync();
                return RedirectToEntity(entityId, entityType);
            }
            return RedirectToAction("Index", "Home");
        }

        private async Task PopulateRelatedEntityDropdown(Guid entityId, string entityType, Guid? selectedId = null)
        {
            if (entityType == EntityTypes.Person)
            {
                List<Contact> all = await _repository.ListAsync<Contact>();
                List<Contact> available = all.Where(p => p.Id != entityId).OrderBy(p => p.FullName).ToList();
                ViewData["RelatedEntityId"] = new SelectList(available, "Id", "FullName", selectedId);
            }
            else if (entityType == EntityTypes.Company)
            {
                List<Employer> all = await _repository.ListAsync<Employer>();
                List<Employer> available = all.Where(c => c.Id != entityId).OrderBy(c => c.CompanyName).ToList();
                ViewData["RelatedEntityId"] = new SelectList(available, "Id", "CompanyName", selectedId);
            }
        }

        private List<SelectListItem> GetRelationshipTypeOptions(string entityType, string? selectedValue = null)
        {
            List<RelationshipTypeDefinition> types = RelationshipTypeService.GetByEntityType(entityType);
            // Sort by Category then Name
            types = types.OrderBy(t => t.Category).ThenBy(t => t.Name).ToList();

            List<SelectListItem> options = new();
            Dictionary<string, SelectListGroup> groups = new();

            foreach (RelationshipTypeDefinition t in types)
            {
                if (!groups.ContainsKey(t.Category))
                {
                    groups[t.Category] = new SelectListGroup { Name = t.Category };
                }
                SelectListGroup group = groups[t.Category];

                string fwdText = t.IsSymmetric ? $"is {t.Name} of" : $"is {t.Name} of ({t.OppositeName})";
                options.Add(new SelectListItem
                {
                    Value = $"{t.Id}_Fwd",
                    Text = fwdText,
                    Group = group,
                    Selected = selectedValue == $"{t.Id}_Fwd"
                });

                if (!t.IsSymmetric)
                {
                    string revText = $"is {t.OppositeName} of ({t.Name})";
                    options.Add(new SelectListItem
                    {
                        Value = $"{t.Id}_Rev",
                        Text = revText,
                        Group = group,
                        Selected = selectedValue == $"{t.Id}_Rev"
                    });
                }
            }

            return options;
        }
    }
}
