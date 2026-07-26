using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Rvnx.CRM.Core.DTOs.Contact;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Web.Constants;
using Rvnx.CRM.Web.Controllers.Base;
using Rvnx.CRM.Web.ViewModels.Merge;

namespace Rvnx.CRM.Web.Controllers;

public class MergeController(
    IContactReadService contactReadService,
    IMergeService mergeService) : AuthorizedController
{
    private readonly IContactReadService _contactReadService = contactReadService;
    private readonly IMergeService _mergeService = mergeService;

    [HttpGet]
    public async Task<IActionResult> Index(Guid primaryId)
    {
        ContactDetailDto? primaryContact = await _contactReadService.GetContactDetailsAsync(primaryId);
        if (primaryContact == null)
        {
            return NotFound("Primary contact not found.");
        }

        List<ContactDto> allContacts = await _contactReadService.GetIndexDataAsync(false);
        List<ContactDto> availableContacts = allContacts.Where(c => c.Id != primaryId).ToList();

        ViewBag.SecondaryContacts = new SelectList(availableContacts, "Id", "FullName");

        MergeContactViewModel model = new()
        {
            PrimaryContactId = primaryId,
            PrimaryContact = primaryContact
        };

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Confirm(MergeContactViewModel model)
    {
        if (model.PrimaryContactId == model.SecondaryContactId)
        {
            ModelState.AddModelError("", "Cannot merge a contact with itself.");
            return RedirectToAction(nameof(Index), new { primaryId = model.PrimaryContactId });
        }

        ContactDetailDto? primaryContact = await _contactReadService.GetContactDetailsAsync(model.PrimaryContactId);
        ContactDetailDto? secondaryContact = await _contactReadService.GetContactDetailsAsync(model.SecondaryContactId);

        if (primaryContact == null || secondaryContact == null)
        {
            return NotFound("One or both contacts not found.");
        }

        model.PrimaryContact = primaryContact;
        model.SecondaryContact = secondaryContact;

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> ExecuteMerge(Guid primaryContactId, Guid secondaryContactId)
    {
        if (primaryContactId == secondaryContactId)
        {
            TempData[TempDataKeys.ErrorMessage] = "Cannot merge a contact with itself.";
            return RedirectToAction(nameof(Index), new { primaryId = primaryContactId });
        }

        try
        {
            await _mergeService.MergeContactsAsync(primaryContactId, secondaryContactId);
            TempData[TempDataKeys.SuccessMessage] = "Contacts merged successfully.";
            return RedirectToAction("Details", "Contacts", new { id = primaryContactId });
        }
        catch (Exception ex)
        {
            TempData[TempDataKeys.ErrorMessage] = $"An error occurred during merge: {ex.Message}";
            return RedirectToAction("Details", "Contacts", new { id = primaryContactId });
        }
    }
}
