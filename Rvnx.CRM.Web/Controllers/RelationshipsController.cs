using Microsoft.AspNetCore.Mvc;
using Rvnx.CRM.Core.DTOs.Contact;
using Rvnx.CRM.Core.Extensions;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Web.Controllers.Base;

namespace Rvnx.CRM.Web.Controllers
{
    public class RelationshipsController(IRepository repository, IRelationshipService relationshipService)
        : RepositoryController(repository)
    {
        private readonly IRelationshipService _relationshipService = relationshipService;

        public async Task<IActionResult> Create(Guid entityId, string entityType)
        {
            if (entityId == Guid.Empty || string.IsNullOrEmpty(entityType))
            {
                return NotFound();
            }

            RelationshipFormViewModel viewModel = new()
            {
                EntityId = entityId,
                EntityType = entityType,
                EntityName = await GetEntityName(entityId, entityType),
                RelatedEntityOptions =
                    await _relationshipService.GetRelatedEntityOptionsAsync(entityId, entityType),
                RelationshipTypeOptions = _relationshipService.GetRelationshipTypeOptions(entityType)
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(RelationshipFormViewModel viewModel)
        {
            if (string.IsNullOrEmpty(viewModel.SelectedRelationshipType))
            {
                ModelState.AddModelError("SelectedRelationshipType", "Relationship Type is required.");
            }

            if (ModelState.IsValid)
            {
                Relationship relationship = viewModel.ToEntity();
                RelationshipOperationResult result =
                    await _relationshipService.CreateRelationshipAsync(relationship,
                        viewModel.SelectedRelationshipType);
                if (result.Success)
                {
                    return RedirectToEntity(result.RedirectId, result.EntityType ?? string.Empty);
                }
                else
                {
                    ModelState.AddModelError("SelectedRelationshipType",
                        result.ErrorMessage ?? "Invalid Relationship Type.");
                }
            }

            viewModel.EntityName = await GetEntityName(viewModel.EntityId, viewModel.EntityType);
            viewModel.RelatedEntityOptions = await _relationshipService.GetRelatedEntityOptionsAsync(viewModel.EntityId,
                viewModel.EntityType, viewModel.RelatedEntityId);
            viewModel.RelationshipTypeOptions =
                _relationshipService.GetRelationshipTypeOptions(viewModel.EntityType,
                    viewModel.SelectedRelationshipType);

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePartial(Guid entityId, string entityType, CreatePartialContactRelationshipDto dto)
        {
            if (entityId == Guid.Empty || string.IsNullOrEmpty(entityType))
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                Relationship relationship = new()
                {
                    Id = Guid.NewGuid(),
                    EntityId = entityId,
                    EntityType = entityType,
                    RelatedEntityId = null,
                    PartialContactFirstName = dto.PartialContactFirstName,
                    PartialContactLastName = dto.PartialContactLastName,
                    PartialContactDateOfBirth = dto.PartialContactDateOfBirth,
                    Description = dto.Description
                };

                RelationshipOperationResult result =
                    await _relationshipService.CreateRelationshipAsync(relationship, dto.SelectedRelationshipType);

                if (result.Success)
                {
                    return RedirectToEntity(result.RedirectId, result.EntityType ?? string.Empty);
                }
                else
                {
                    ModelState.AddModelError("SelectedRelationshipType", result.ErrorMessage ?? "Invalid Relationship Type.");
                }
            }

            RelationshipFormViewModel viewModel = new()
            {
                EntityId = entityId,
                EntityType = entityType,
                EntityName = await GetEntityName(entityId, entityType),
                RelatedEntityOptions = await _relationshipService.GetRelatedEntityOptionsAsync(entityId, entityType),
                RelationshipTypeOptions = _relationshipService.GetRelationshipTypeOptions(entityType),
                SelectedRelationshipType = dto.SelectedRelationshipType,
                PartialContactFirstName = dto.PartialContactFirstName,
                PartialContactLastName = dto.PartialContactLastName,
                PartialContactDateOfBirth = dto.PartialContactDateOfBirth,
                IsPartialContact = true,
                Description = dto.Description
            };

            return View("Create", viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Promote(Guid id)
        {
            RelationshipOperationResult result = await _relationshipService.PromotePartialContactAsync(id);
            if (result.Success)
            {
                return RedirectToAction("Edit", "Contacts", new { id = result.RedirectId });
            }

            return BadRequest(result.ErrorMessage);
        }

        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            Relationship? relationship = await Repository.GetByIdAsync<Relationship>(id.Value);
            if (relationship == null)
            {
                return NotFound();
            }

            string currentSelection = $"{relationship.RelationshipTypeId}_Fwd";

            RelationshipFormViewModel viewModel = new()
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
                RelatedEntityOptions =
                    await _relationshipService.GetRelatedEntityOptionsAsync(relationship.EntityId,
                        relationship.EntityType, relationship.RelatedEntityId),
                RelationshipTypeOptions =
                    _relationshipService.GetRelationshipTypeOptions(relationship.EntityType, currentSelection),
                SelectedRelationshipType = currentSelection
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, RelationshipFormViewModel viewModel)
        {
            if (id != viewModel.Id)
            {
                return NotFound();
            }

            if (string.IsNullOrEmpty(viewModel.SelectedRelationshipType))
            {
                ModelState.AddModelError("SelectedRelationshipType", "Relationship Type is required.");
            }

            if (ModelState.IsValid)
            {
                Relationship relationship = viewModel.ToEntity();
                RelationshipOperationResult result =
                    await _relationshipService.UpdateRelationshipAsync(id, relationship,
                        viewModel.SelectedRelationshipType);
                if (result.Success)
                {
                    return RedirectToEntity(result.RedirectId, result.EntityType ?? string.Empty);
                }
                else
                {
                    ModelState.AddModelError("SelectedRelationshipType",
                        result.ErrorMessage ?? "Invalid Relationship Type.");
                }
            }

            viewModel.EntityName = await GetEntityName(viewModel.EntityId, viewModel.EntityType);
            viewModel.RelatedEntityOptions = await _relationshipService.GetRelatedEntityOptionsAsync(viewModel.EntityId,
                viewModel.EntityType, viewModel.RelatedEntityId);
            viewModel.RelationshipTypeOptions =
                _relationshipService.GetRelationshipTypeOptions(viewModel.EntityType,
                    viewModel.SelectedRelationshipType);

            return View(viewModel);
        }

        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            Relationship? relationship = await Repository.GetByIdAsync<Relationship>(id.Value);
            if (relationship == null)
            {
                return NotFound();
            }

            // Populate Person/RelatedPerson so names show up
            relationship.Person = await Repository.GetByIdAsync<Contact>(relationship.EntityId);
            if (relationship.RelatedEntityId != null)
            {
                relationship.RelatedPerson = await Repository.GetByIdAsync<Contact>(relationship.RelatedEntityId.Value);
            }

            RelationshipDto viewModel = relationship.ToDto();
            RelationshipDeleteViewModel deleteViewModel = new()
            {
                Id = viewModel.Id,
                EntityId = viewModel.EntityId,
                EntityType = viewModel.EntityType,
                RelatedEntityId = viewModel.RelatedEntityId,
                RelationshipTypeId = viewModel.RelationshipTypeId,
                RelationshipTypeName = viewModel.RelationshipTypeName,
                RelationshipTypeOppositeName = viewModel.RelationshipTypeOppositeName,
                RelatedEntityName = viewModel.RelatedEntityName,
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
            Relationship? relationship = await Repository.GetByIdAsync<Relationship>(id);
            if (relationship != null)
            {
                Guid entityId = relationship.EntityId;
                string entityType = relationship.EntityType;
                await Repository.DeleteAsync<Relationship>(id);
                await Repository.SaveChangesAsync();
                return RedirectToEntity(entityId, entityType);
            }

            return RedirectToAction("Index", "Home");
        }
    }
}
