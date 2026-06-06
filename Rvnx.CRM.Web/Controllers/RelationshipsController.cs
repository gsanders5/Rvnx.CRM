using Microsoft.AspNetCore.Mvc;
using Rvnx.CRM.Core.DTOs.Contact;
using Rvnx.CRM.Core.Extensions;
using Rvnx.CRM.Web.ViewModels.Contact;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Web.Controllers.Base;

namespace Rvnx.CRM.Web.Controllers;

public class RelationshipsController(
    IRelationshipService relationshipService,
    IRelationshipSuggestionService relationshipSuggestionService,
    IRepository repository,
    IContactLookupService contactLookupService)
    : RepositoryController(repository)
{
    private readonly IRelationshipService _relationshipService = relationshipService;
    private readonly IRelationshipSuggestionService _relationshipSuggestionService = relationshipSuggestionService;
    private readonly IContactLookupService _contactLookupService = contactLookupService;

    [HttpGet]
    public async Task<IActionResult> Create(Guid contactId)
    {
        if (contactId == Guid.Empty || !await _contactLookupService.ExistsAsync(contactId))
        {
            return NotFound();
        }

        RelationshipFormViewModel viewModel = new()
        {
            ContactId = contactId
        };

        await PopulateRelationshipFormOptionsAsync(viewModel);

        return View(viewModel);
    }

    [HttpPost]
    public async Task<IActionResult> Create(RelationshipFormViewModel viewModel)
    {
        if (string.IsNullOrEmpty(viewModel.SelectedRelationshipType))
        {
            ModelState.AddModelError("SelectedRelationshipType", "Relationship Type is required.");
        }

        if (ModelState.IsValid)
        {
            if (!await _contactLookupService.ExistsAsync(viewModel.ContactId))
            {
                return NotFound();
            }

            Relationship relationship = viewModel.ToEntity();
            RelationshipOperationResult result =
                await _relationshipService.CreateRelationshipAsync(relationship,
                    viewModel.SelectedRelationshipType, viewModel.SuggestedRelationships);
            if (result.Success)
            {
                return RedirectToContact(result.RedirectId);
            }

            ModelState.AddModelError("SelectedRelationshipType",
                result.ErrorMessage ?? "Invalid Relationship Type.");
        }

        await PopulateRelationshipFormOptionsAsync(viewModel);
        return View(viewModel);
    }

    private async Task PopulateRelationshipFormOptionsAsync(RelationshipFormViewModel viewModel)
    {
        viewModel.ContactName = await _contactLookupService.GetContactNameAsync(viewModel.ContactId);
        Guid? selectedId = viewModel.RelatedContactId == Guid.Empty ? null : viewModel.RelatedContactId;
        viewModel.RelatedContactOptions = await _relationshipService.GetRelatedContactOptionsAsync(
            viewModel.ContactId, selectedId);
        viewModel.RelationshipTypeOptions = _relationshipService.GetRelationshipTypeOptions(
            viewModel.SelectedRelationshipType);
        viewModel.RelationshipTypes = _relationshipService.GetRelationshipTypes();
    }

    [HttpPost]
    public async Task<IActionResult> CreatePartial(Guid contactId, CreatePartialContactRelationshipDto dto)
    {
        if (contactId == Guid.Empty)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            if (!await _contactLookupService.ExistsAsync(contactId))
            {
                return NotFound();
            }

            RelationshipOperationResult result =
                await _relationshipService.CreatePartialContactRelationshipAsync(contactId,
                    dto.SelectedRelationshipType, dto);
            if (result.Success)
            {
                return RedirectToContact(result.RedirectId);
            }

            ModelState.AddModelError(string.Empty,
                result.ErrorMessage ?? "Failed to create partial contact relationship.");
        }

        TempData["ErrorMessage"] =
            string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
        return RedirectToAction(nameof(Create), new { contactId });
    }

    [HttpGet]
    public async Task<IActionResult> GetSuggestions(Guid contactId, Guid? relatedContactId, string relationshipType,
        string? partialContactName = null)
    {
        if (string.IsNullOrEmpty(relationshipType))
        {
            return Json(new List<SuggestedRelationshipDto>());
        }

        string[] parts = relationshipType.Split('_');
        if (parts.Length != 2 || !Guid.TryParse(parts[0], out Guid typeId))
        {
            return Json(new List<SuggestedRelationshipDto>());
        }

        bool isReverse = parts[1] == "Rev";

        List<SuggestedRelationshipDto> suggestions =
            await _relationshipSuggestionService.GetSuggestedRelationshipsAsync(contactId, relatedContactId, typeId, isReverse,
                partialContactName);
        return Json(suggestions);
    }

    [HttpPost]
    public async Task<IActionResult> Promote(Guid contactId)
    {
        if (contactId == Guid.Empty)
        {
            return NotFound();
        }

        RelationshipOperationResult result = await _relationshipService.PromotePartialContactAsync(contactId);

        return result.Success
            ? RedirectToAction("Edit", "Contacts", new { id = result.RedirectId })
            : RedirectToAction("Index", "Contacts");
    }

    [HttpGet]
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
        HashSet<Guid> partialIds = await _contactLookupService.GetPartialContactIdsAsync(
            [relationship.ContactId, relationship.RelatedContactId]);

        RelationshipFormViewModel viewModel = new()
        {
            Id = relationship.Id,
            ContactId = relationship.ContactId,
            RelatedContactId = relationship.RelatedContactId,
            RelationshipTypeId = relationship.RelationshipTypeId,
            Description = relationship.Description,
            StartDate = relationship.StartDate,
            EndDate = relationship.EndDate,
            SelectedRelationshipType = currentSelection,
            IsContactPartial = partialIds.Contains(relationship.ContactId),
            IsRelatedContactPartial = partialIds.Contains(relationship.RelatedContactId)
        };

        await PopulateRelationshipFormOptionsAsync(viewModel);
        return View(viewModel);
    }

    [HttpPost]
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
            if (!await _contactLookupService.ExistsAsync(viewModel.ContactId))
            {
                return NotFound();
            }

            Relationship relationship = viewModel.ToEntity();
            RelationshipOperationResult result =
                await _relationshipService.UpdateRelationshipAsync(id, relationship,
                    viewModel.SelectedRelationshipType);
            if (result.Success)
            {
                return RedirectToContact(result.RedirectId);
            }

            ModelState.AddModelError("SelectedRelationshipType",
                result.ErrorMessage ?? "Invalid Relationship Type.");
        }

        await PopulateRelationshipFormOptionsAsync(viewModel);
        return View(viewModel);
    }

    [HttpGet]
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

        if (!await _contactLookupService.ExistsAsync(relationship.ContactId))
        {
            return NotFound();
        }

        string? safeReturnUrl = !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl) ? returnUrl : null;

        return View(new RelationshipDeleteViewModel(relationship.ToDto(), safeReturnUrl));
    }

    [HttpPost, ActionName("Delete")]
    public async Task<IActionResult> DeleteConfirmed(Guid id, string? returnUrl = null)
    {
        OperationResult result = await _relationshipService.DeleteRelationshipAsync(id);
        return result.Success
            ? !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)
                ? LocalRedirect(returnUrl)
                : RedirectToContact(result.RedirectId)
            : RedirectToAction("Index", "Home");
    }
}
