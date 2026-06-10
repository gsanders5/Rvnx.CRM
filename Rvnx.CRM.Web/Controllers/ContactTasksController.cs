using Microsoft.AspNetCore.Mvc;
using Rvnx.CRM.Core.DTOs.Contact;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models;
using Rvnx.CRM.Web.Controllers.Base;

namespace Rvnx.CRM.Web.Controllers;

public class ContactTasksController(IContactTaskService contactTaskService, IRepository repository) : RepositoryController(repository)
{
    private readonly IContactTaskService _contactTaskService = contactTaskService;

    [HttpGet]
    public async Task<IActionResult> Create(Guid contactId)
    {
        ContactTaskFormDto? dto = await _contactTaskService.GetFormForCreateAsync(contactId);
        return dto == null ? NotFound() : View(dto);
    }

    [HttpPost]
    public async Task<IActionResult> Create(ContactTaskFormDto dto)
    {
        if (ModelState.IsValid)
        {
            IActionResult? handled = HandleOperationResult(await _contactTaskService.CreateAsync(dto));
            if (handled != null)
            {
                return handled;
            }
        }

        return View(dto);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id)
    {
        ContactTaskFormDto? dto = await _contactTaskService.GetFormAsync(id);
        return dto == null ? NotFound() : View(dto);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(Guid id, ContactTaskFormDto dto)
    {
        if (id != dto.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            IActionResult? handled = HandleOperationResult(await _contactTaskService.UpdateAsync(id, dto));
            if (handled != null)
            {
                return handled;
            }
        }

        return View(dto);
    }

    [HttpPost, ActionName("Delete")]
    public async Task<IActionResult> DeleteConfirmed(Guid id)
    {
        OperationResult result = await _contactTaskService.DeleteAsync(id);
        return result.Success
            ? RedirectToContact(result.RedirectId)
            : RedirectToAction("Index", "Contacts");
    }

    [HttpPost]
    public async Task<IActionResult> ToggleComplete(Guid id)
    {
        OperationResult result = await _contactTaskService.ToggleCompleteAsync(id);
        return result.Success
            ? RedirectToContact(result.RedirectId)
            : NotFound();
    }
}
