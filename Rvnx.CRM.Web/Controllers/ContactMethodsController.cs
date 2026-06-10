using Microsoft.AspNetCore.Mvc;
using Rvnx.CRM.Core.DTOs.Contact;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models;
using Rvnx.CRM.Web.Controllers.Base;

namespace Rvnx.CRM.Web.Controllers;

public class ContactMethodsController(IContactMethodService contactMethodService, IRepository repository)
    : RepositoryController(repository)
{
    private readonly IContactMethodService _contactMethodService = contactMethodService;

    [HttpGet]
    public async Task<IActionResult> Create(Guid contactId)
    {
        ContactMethodFormDto? dto = await _contactMethodService.GetFormForCreateAsync(contactId);
        return dto == null ? NotFound() : View(dto);
    }

    [HttpPost]
    public async Task<IActionResult> Create(ContactMethodFormDto contactInfoInput)
    {
        if (ModelState.IsValid)
        {
            IActionResult? handled = HandleOperationResult(await _contactMethodService.CreateAsync(contactInfoInput));
            if (handled != null)
            {
                return handled;
            }
        }

        return View(contactInfoInput);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        ContactMethodFormDto? dto = await _contactMethodService.GetFormAsync(id.Value);
        return dto == null ? NotFound() : View(dto);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(Guid id, ContactMethodFormDto contactInfoInput)
    {
        if (id != contactInfoInput.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            IActionResult? handled = HandleOperationResult(await _contactMethodService.UpdateAsync(id, contactInfoInput));
            if (handled != null)
            {
                return handled;
            }
        }

        return View(contactInfoInput);
    }

    [HttpPost, ActionName("Delete")]
    public async Task<IActionResult> DeleteConfirmed(Guid id)
    {
        OperationResult result = await _contactMethodService.DeleteAsync(id);
        return result.Success
            ? RedirectToContact(result.RedirectId)
            : RedirectToAction("Index", "Home");
    }
}
