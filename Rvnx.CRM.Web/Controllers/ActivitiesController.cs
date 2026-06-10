using Microsoft.AspNetCore.Mvc;
using Rvnx.CRM.Core.DTOs.Contact;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models;
using Rvnx.CRM.Web.Controllers.Base;

namespace Rvnx.CRM.Web.Controllers;

public class ActivitiesController(IActivityService activityService, IRepository repository, IContactReadService contactReadService) : RepositoryController(repository)
{
    private readonly IActivityService _activityService = activityService;
    private readonly IContactReadService _contactReadService = contactReadService;

    [HttpGet]
    public async Task<IActionResult> Create(Guid contactId)
    {
        ActivityFormDto? dto = await _activityService.GetFormForCreateAsync(contactId);
        if (dto == null)
        {
            return NotFound();
        }

        await PopulateContactsSelectListExcludeDeceased(_contactReadService, dto.ContactIds);
        return View(dto);
    }

    [HttpPost]
    public async Task<IActionResult> Create(ActivityFormDto dto)
    {
        if (ModelState.IsValid)
        {
            IActionResult? handled = HandleOperationResult(await _activityService.CreateAsync(dto));
            if (handled != null)
            {
                return handled;
            }
        }

        await PopulateContactsSelectListExcludeDeceased(_contactReadService, dto.ContactIds);
        return View(dto);
    }

    [HttpPost]
    public async Task<IActionResult> QuickLog(Guid contactId, string activityType)
    {
        OperationResult result = await _activityService.QuickLogAsync(contactId, activityType);
        return result.Success
            ? RedirectToContact(result.RedirectId)
            : result.IsNotFound ? NotFound() : BadRequest(result.ErrorMessage);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id)
    {
        ActivityFormDto? dto = await _activityService.GetFormAsync(id);
        if (dto == null)
        {
            return NotFound();
        }

        await PopulateContactsSelectListExcludeDeceased(_contactReadService, dto.ContactIds);
        return View(dto);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(Guid id, ActivityFormDto dto)
    {
        if (id != dto.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            IActionResult? handled = HandleOperationResult(await _activityService.UpdateAsync(id, dto));
            if (handled != null)
            {
                return handled;
            }
        }

        await PopulateContactsSelectListExcludeDeceased(_contactReadService, dto.ContactIds);
        return View(dto);
    }

    [HttpPost, ActionName("Delete")]
    public async Task<IActionResult> DeleteConfirmed(Guid id)
    {
        OperationResult result = await _activityService.DeleteAsync(id);
        return result.Success
            ? RedirectToContact(result.RedirectId)
            : RedirectToAction("Index", "Contacts");
    }

}
