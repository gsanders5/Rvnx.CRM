using Microsoft.AspNetCore.Mvc;
using Rvnx.CRM.Core.Constants;
using Rvnx.CRM.Core.DTOs.Contact;
using Rvnx.CRM.Core.Extensions;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Web.Controllers.Base;

namespace Rvnx.CRM.Web.Controllers
{
    public class RelationshipsController(IRelationshipService relationshipService, IRepository repository)
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
                RelationshipTypeOptions = _relationshipService.GetRelationshipTypeOptions(entityType),
                RelationshipTypes = _relationshipService.GetRelationshipTypes(entityType)
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
            viewModel.RelationshipTypes = _relationshipService.GetRelationshipTypes(viewModel.EntityType);

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePartial(Guid entityId, string entityType, CreatePartialContactRelationshipDto dto)
        {
            if (entityId == Guid.Empty || string.IsNullOrEmpty(entityType) || entityType != EntityTypes.Person)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                RelationshipOperationResult result = await _relationshipService.CreatePartialContactRelationshipAsync(entityId, dto.SelectedRelationshipType, dto);
                if (result.Success)
                {
                    return RedirectToEntity(result.RedirectId, result.EntityType ?? string.Empty);
                }

                ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Failed to create partial contact relationship.");
            }

            // If we fail, we need to redirect back to the Create view to show errors, 
            // but since it's a different action we'll pass an error in TempData and redirect.
            TempData["ErrorMessage"] = string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
            return RedirectToAction(nameof(Create), new { entityId, entityType });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Promote(Guid contactId)
        {
            if (contactId == Guid.Empty)
            {
                return NotFound();
            }

            RelationshipOperationResult result = await _relationshipService.PromotePartialContactAsync(contactId);

            if (result.Success)
            {
                // Redirect to the newly promoted contact's edit page
                return RedirectToAction("Edit", "Contacts", new { id = result.RedirectId });
            }

            // If promotion fails (e.g. not found, or not partial), redirect safely
            return RedirectToAction("Index", "Contacts");
        }

        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            Relationship? relationship = await _relationshipService.GetRelationshipForEditAsync(id.Value);
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
                RelationshipTypes = _relationshipService.GetRelationshipTypes(relationship.EntityType),
                SelectedRelationshipType = currentSelection,
                IsEntityPartial = await IsPartialContactAsync(relationship.EntityId),
                IsRelatedEntityPartial = await IsPartialContactAsync(relationship.RelatedEntityId)
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
            viewModel.RelationshipTypes = _relationshipService.GetRelationshipTypes(viewModel.EntityType);

            return View(viewModel);
        }

        public async Task<IActionResult> Delete(Guid? id, string? returnUrl = null)
        {
            if (id == null)
            {
                return NotFound();
            }

            Relationship? relationship = await _relationshipService.GetRelationshipForDeleteAsync(id.Value);
            if (relationship == null)
            {
                return NotFound();
            }

            RelationshipDto viewModel = relationship.ToDto();

            // Sanitize returnUrl - if it's not local, treat it as null/empty
            string? safeReturnUrl = !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl) ? returnUrl : null;

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
                EndDate = viewModel.EndDate,
                ReturnUrl = safeReturnUrl
            };

            return View(deleteViewModel);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id, string? returnUrl = null)
        {
            OperationResult result = await _relationshipService.DeleteRelationshipAsync(id);
            return result.Success
                ? !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)
                    ? Redirect(returnUrl)
                    : RedirectToEntity(result.RedirectId, result.RedirectType)
                : RedirectToAction("Index", "Home");
        }
    }
}
