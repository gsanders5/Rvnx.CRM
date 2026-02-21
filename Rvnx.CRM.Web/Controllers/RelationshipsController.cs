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

            var viewModel = new RelationshipCreateViewModel
            {
                EntityId = entityId,
                EntityType = entityType,
                EntityName = await GetEntityName(entityId, entityType),
                RelatedEntityOptions = await GetRelatedEntityOptions(entityId, entityType),
                RelationshipTypeOptions = GetRelationshipTypeOptions(entityType)
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(RelationshipCreateViewModel viewModel)
        {
            if (string.IsNullOrEmpty(viewModel.SelectedRelationshipType))
            {
                ModelState.AddModelError("SelectedRelationshipType", "Relationship Type is required.");
            }

            if (ModelState.IsValid)
            {
                string[] parts = viewModel.SelectedRelationshipType.Split('_');
                if (parts.Length == 2 && Guid.TryParse(parts[0], out Guid typeId))
                {
                    string direction = parts[1];
                    viewModel.RelationshipTypeId = typeId;

                    Relationship relationship = viewModel.ToEntity();

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
                    ModelState.AddModelError("SelectedRelationshipType", "Invalid Relationship Type.");
                }
            }

            viewModel.EntityName = await GetEntityName(viewModel.EntityId, viewModel.EntityType);
            viewModel.RelatedEntityOptions = await GetRelatedEntityOptions(viewModel.EntityId, viewModel.EntityType, viewModel.RelatedEntityId);
            viewModel.RelationshipTypeOptions = GetRelationshipTypeOptions(viewModel.EntityType, viewModel.SelectedRelationshipType);

            return View(viewModel);
        }

        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null) return NotFound();

            Relationship? relationship = await _repository.GetByIdAsync<Relationship>(id.Value);
            if (relationship == null) return NotFound();

            string currentSelection = $"{relationship.RelationshipTypeId}_Fwd";

            var viewModel = new RelationshipEditViewModel
            {
                Id = relationship.Id,
                EntityId = relationship.EntityId,
                RelatedEntityId = relationship.RelatedEntityId,
                EntityType = relationship.EntityType,
                RelationshipTypeId = relationship.RelationshipTypeId,
                Description = relationship.Description,
                StartDate = relationship.StartDate,
                EndDate = relationship.EndDate,
                EntityName = await GetEntityName(relationship.EntityId, relationship.EntityType),
                RelatedEntityOptions = await GetRelatedEntityOptions(relationship.EntityId, relationship.EntityType, relationship.RelatedEntityId),
                RelationshipTypeOptions = GetRelationshipTypeOptions(relationship.EntityType, currentSelection),
                SelectedRelationshipType = currentSelection
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, RelationshipEditViewModel viewModel)
        {
            if (id != viewModel.Id) return NotFound();

            if (string.IsNullOrEmpty(viewModel.SelectedRelationshipType))
            {
                ModelState.AddModelError("SelectedRelationshipType", "Relationship Type is required.");
            }

            if (ModelState.IsValid)
            {
                string[] parts = viewModel.SelectedRelationshipType.Split('_');
                if (parts.Length == 2 && Guid.TryParse(parts[0], out Guid typeId))
                {
                    string direction = parts[1];
                    // Update DTO with selected type ID
                    viewModel.RelationshipTypeId = typeId;

                    Relationship? existingRelationship = await _repository.GetByIdAsync<Relationship>(id);
                    if (existingRelationship == null) return NotFound();

                    // Apply DTO updates to entity
                    existingRelationship.UpdateEntity(viewModel);

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
                    ModelState.AddModelError("SelectedRelationshipType", "Invalid Relationship Type.");
                }
            }

            viewModel.EntityName = await GetEntityName(viewModel.EntityId, viewModel.EntityType);
            viewModel.RelatedEntityOptions = await GetRelatedEntityOptions(viewModel.EntityId, viewModel.EntityType, viewModel.RelatedEntityId);
            viewModel.RelationshipTypeOptions = GetRelationshipTypeOptions(viewModel.EntityType, viewModel.SelectedRelationshipType);

            return View(viewModel);
        }

        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null) return NotFound();
            Relationship? relationship = await _repository.GetByIdAsync<Relationship>(id.Value);
            if (relationship == null) return NotFound();

            // Populate Person/RelatedPerson so names show up
            relationship.Person = await _repository.GetByIdAsync<Contact>(relationship.EntityId);
            relationship.RelatedPerson = await _repository.GetByIdAsync<Contact>(relationship.RelatedEntityId);

            var viewModel = relationship.ToDto();
            // Since ToDto() doesn't include EntityName (it puts it in EntityName property of Dto?), wait.
            // RelationshipDto has EntityName and RelatedEntityName.
            // Let's use RelationshipDeleteViewModel which inherits RelationshipDto.
            var deleteViewModel = new RelationshipDeleteViewModel
            {
                Id = viewModel.Id,
                EntityId = viewModel.EntityId,
                EntityType = viewModel.EntityType,
                RelatedEntityId = viewModel.RelatedEntityId,
                RelationshipTypeId = viewModel.RelationshipTypeId,
                RelationshipTypeName = viewModel.RelationshipTypeName,
                RelationshipTypeOppositeName = viewModel.RelationshipTypeOppositeName,
                RelatedEntityName = viewModel.RelatedEntityName,
                // EntityName in Dto is Person.FullName.
                EntityName = viewModel.EntityName,
                Description = viewModel.Description,
                StartDate = viewModel.StartDate,
                EndDate = viewModel.EndDate
            };

            return View(deleteViewModel);
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

        private async Task<List<SelectOptionDto>> GetRelatedEntityOptions(Guid entityId, string entityType, Guid? selectedId = null)
        {
            var options = new List<SelectOptionDto>();

            if (entityType == EntityTypes.Person)
            {
                List<Contact> available = await _repository.ListAsNoTrackingAsync<Contact>(p => p.Id != entityId);
                available = available.OrderBy(p => p.FullName).ToList();
                options = available.Select(p => new SelectOptionDto
                {
                    Value = p.Id.ToString(),
                    Text = p.FullName,
                    Selected = selectedId == p.Id
                }).ToList();
            }
            else if (entityType == EntityTypes.Company)
            {
                List<Employer> available = await _repository.ListAsNoTrackingAsync<Employer>(c => c.Id != entityId);
                available = available.OrderBy(c => c.CompanyName).ToList();
                options = available.Select(c => new SelectOptionDto
                {
                    Value = c.Id.ToString(),
                    Text = c.CompanyName,
                    Selected = selectedId == c.Id
                }).ToList();
            }
            return options;
        }

        private List<SelectOptionDto> GetRelationshipTypeOptions(string entityType, string? selectedValue = null)
        {
            List<RelationshipTypeDefinition> types = RelationshipTypeService.GetByEntityType(entityType);
            // Sort by Category then Name
            types = types.OrderBy(t => t.Category).ThenBy(t => t.Name).ToList();

            List<SelectOptionDto> options = new();

            foreach (RelationshipTypeDefinition t in types)
            {
                string group = t.Category;

                string fwdText = t.IsSymmetric ? $"is {t.Name} of" : $"is {t.Name} of ({t.OppositeName})";
                options.Add(new SelectOptionDto
                {
                    Value = $"{t.Id}_Fwd",
                    Text = fwdText,
                    Group = group,
                    Selected = selectedValue == $"{t.Id}_Fwd"
                });

                if (!t.IsSymmetric)
                {
                    string revText = $"is {t.OppositeName} of ({t.Name})";
                    options.Add(new SelectOptionDto
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
