using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Rvnx.CRM.Core.Constants;
using Rvnx.CRM.Core.DTOs.Common;
using Rvnx.CRM.Core.Extensions;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models.Business;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Core.Services;
using Rvnx.CRM.Web.Controllers.Base;

namespace Rvnx.CRM.Web.Controllers
{
    public class RelationshipsController(IRepository repository) : RepositoryController(repository)
    {
        public async Task<IActionResult> Create(Guid entityId, string entityType)
        {
            if (entityId == Guid.Empty || string.IsNullOrEmpty(entityType)) return NotFound();

            ViewData["EntityName"] = await GetEntityName(entityId, entityType);
            ViewData["EntityId"] = entityId;
            ViewData["EntityType"] = entityType;

            await PopulateRelatedEntityDropdown(entityId, entityType);
            ViewData["RelationshipTypeSelection"] = GetRelationshipTypeOptions(entityType);

            return View(new RelationshipFormDto { EntityId = entityId, EntityType = entityType });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(RelationshipFormDto relationshipDto, string relationshipTypeSelection)
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
                    relationshipDto.RelationshipTypeId = typeId;

                    Relationship relationship = relationshipDto.ToEntity();

                    if (direction == "Rev")
                    {
                        Guid temp = relationship.EntityId;
                        relationship.EntityId = relationship.RelatedEntityId;
                        relationship.RelatedEntityId = temp;
                    }

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

            ViewData["EntityName"] = await GetEntityName(relationshipDto.EntityId, relationshipDto.EntityType);
            ViewData["EntityId"] = relationshipDto.EntityId;
            ViewData["EntityType"] = relationshipDto.EntityType;

            await PopulateRelatedEntityDropdown(relationshipDto.EntityId, relationshipDto.EntityType, relationshipDto.RelatedEntityId);
            ViewData["RelationshipTypeSelection"] = GetRelationshipTypeOptions(relationshipDto.EntityType, relationshipTypeSelection);

            return View(relationshipDto);
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

            RelationshipFormDto dto = new()
            {
                Id = relationship.Id,
                EntityId = relationship.EntityId,
                RelatedEntityId = relationship.RelatedEntityId,
                EntityType = relationship.EntityType,
                RelationshipTypeId = relationship.RelationshipTypeId,
                Description = relationship.Description,
                StartDate = relationship.StartDate,
                EndDate = relationship.EndDate
            };

            return View(dto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, RelationshipFormDto relationshipDto, string relationshipTypeSelection)
        {
            if (id != relationshipDto.Id) return NotFound();

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
                    // Update DTO with selected type ID
                    relationshipDto.RelationshipTypeId = typeId;

                    Relationship? existingRelationship = await _repository.GetByIdAsync<Relationship>(id);
                    if (existingRelationship == null) return NotFound();

                    // Apply DTO updates to entity
                    existingRelationship.UpdateEntity(relationshipDto);

                    if (direction == "Rev")
                    {
                        Guid temp = existingRelationship.EntityId;
                        existingRelationship.EntityId = existingRelationship.RelatedEntityId;
                        existingRelationship.RelatedEntityId = temp;
                    }

                    await _repository.UpdateAsync(existingRelationship);
                    await _repository.SaveChangesAsync();

                    Guid redirectId = direction == "Rev" ? existingRelationship.RelatedEntityId : existingRelationship.EntityId;
                    return RedirectToEntity(redirectId, existingRelationship.EntityType);
                }
                else
                {
                    ModelState.AddModelError("RelationshipTypeSelection", "Invalid Relationship Type.");
                }
            }

            ViewData["EntityName"] = await GetEntityName(relationshipDto.EntityId, relationshipDto.EntityType);
            ViewData["EntityId"] = relationshipDto.EntityId;
            ViewData["EntityType"] = relationshipDto.EntityType;

            await PopulateRelatedEntityDropdown(relationshipDto.EntityId, relationshipDto.EntityType, relationshipDto.RelatedEntityId);
            ViewData["RelationshipTypeSelection"] = GetRelationshipTypeOptions(relationshipDto.EntityType, relationshipTypeSelection);

            return View(relationshipDto);
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
