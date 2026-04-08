using Microsoft.AspNetCore.Mvc;
using Rvnx.CRM.Core.DTOs.Contact;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Web.Controllers.Base;

namespace Rvnx.CRM.Web.Controllers;

public class ContactMethodsController(IContactMethodService contactMethodService, IRepository repository)
    : RepositoryController(repository)
{
    private readonly IContactMethodService _contactMethodService = contactMethodService;

    [HttpGet]
    public async Task<IActionResult> Create(Guid entityId, string entityType)
    {
        ContactMethodFormDto? dto = await _contactMethodService.GetFormForCreateAsync(entityId, entityType);
        return dto == null ? NotFound() : View(dto);
    }

    [HttpPost]
    public async Task<IActionResult> Create(ContactMethodFormDto contactInfoInput)
    {
        if (ModelState.IsValid)
        {
            OperationResult result = await _contactMethodService.CreateAsync(contactInfoInput);
            if (result.Success)
            {
                return RedirectToEntity(result.RedirectId, result.RedirectType);
            }

            if (result.ErrorMessage == "Contact not found.")
            {
                return NotFound();
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
            OperationResult result = await _contactMethodService.UpdateAsync(id, contactInfoInput);
            if (result.Success)
            {
                return RedirectToEntity(result.RedirectId, result.RedirectType);
            }

            if (result.ErrorMessage == "Contact method not found.")
            {
                return NotFound();
            }
        }

        return View(contactInfoInput);
    }

    [HttpGet]
    public async Task<IActionResult> Delete(Guid? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        ContactMethod? contactInfo = await _contactMethodService.GetByIdAsync(id.Value);
        return contactInfo == null ? NotFound() : View(contactInfo);
    }

    [HttpPost, ActionName("Delete")]
    public async Task<IActionResult> DeleteConfirmed(Guid id)
    {
        OperationResult result = await _contactMethodService.DeleteAsync(id);
        return result.Success
            ? RedirectToEntity(result.RedirectId, result.RedirectType)
            : RedirectToAction("Index", "Home");
    }
}