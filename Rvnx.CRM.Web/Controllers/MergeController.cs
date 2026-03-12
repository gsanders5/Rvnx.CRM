using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Web.Controllers.Base;
using Rvnx.CRM.Web.Models;

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
        var primaryContact = await _contactReadService.GetContactDetailsAsync(primaryId);
        if (primaryContact == null)
        {
            return NotFound("Primary contact not found.");
        }

        // We need a list of other contacts to select from
        var allContacts = await _contactReadService.GetIndexDataAsync(false);
        var availableContacts = allContacts.Where(c => c.Id != primaryId).ToList();

        ViewBag.SecondaryContacts = new SelectList(availableContacts, "Id", "FullName");

        var model = new MergeContactViewModel
        {
            PrimaryContactId = primaryId,
            PrimaryContact = primaryContact
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Confirm(MergeContactViewModel model)
    {
        if (model.PrimaryContactId == model.SecondaryContactId)
        {
            ModelState.AddModelError("", "Cannot merge a contact with itself.");
            return RedirectToAction(nameof(Index), new { primaryId = model.PrimaryContactId });
        }

        var primaryContact = await _contactReadService.GetContactDetailsAsync(model.PrimaryContactId);
        var secondaryContact = await _contactReadService.GetContactDetailsAsync(model.SecondaryContactId);

        if (primaryContact == null || secondaryContact == null)
        {
            return NotFound("One or both contacts not found.");
        }

        model.PrimaryContact = primaryContact;
        model.SecondaryContact = secondaryContact;

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ExecuteMerge(Guid primaryContactId, Guid secondaryContactId)
    {
        if (primaryContactId == secondaryContactId)
        {
            TempData["Error"] = "Cannot merge a contact with itself.";
            return RedirectToAction(nameof(Index), new { primaryId = primaryContactId });
        }

        try
        {
            await _mergeService.MergeContactsAsync(primaryContactId, secondaryContactId);
            TempData["Message"] = "Contacts merged successfully.";
            return RedirectToAction("Details", "Contacts", new { id = primaryContactId });
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"An error occurred during merge: {ex.Message}";
            return RedirectToAction("Details", "Contacts", new { id = primaryContactId });
        }
    }
}
