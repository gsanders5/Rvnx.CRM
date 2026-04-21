using Microsoft.AspNetCore.Mvc;
using Rvnx.CRM.Core.DTOs.Contact;
using Rvnx.CRM.Core.Extensions;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Web.Controllers.Base;

namespace Rvnx.CRM.Web.Controllers;

public class ContactTasksController(IContactTaskService contactTaskService, IRepository repository) : RepositoryController(repository)
{
    private readonly IContactTaskService _contactTaskService = contactTaskService;

    [HttpGet]
    public async Task<IActionResult> Create(Guid entityId)
    {
        ContactTaskFormDto? dto = await _contactTaskService.GetFormForCreateAsync(entityId);
        return dto == null ? NotFound() : View(dto);
    }

    [HttpPost]
    public async Task<IActionResult> Create(ContactTaskFormDto dto)
    {
        if (ModelState.IsValid)
        {
            OperationResult result = await _contactTaskService.CreateAsync(dto);
            if (result.Success)
            {
                return RedirectToEntity(result.RedirectId, result.RedirectType);
            }

            if (result.IsNotFound)
            {
                return NotFound();
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
            OperationResult result = await _contactTaskService.UpdateAsync(id, dto);
            if (result.Success)
            {
                return RedirectToEntity(result.RedirectId, result.RedirectType);
            }

            if (result.IsNotFound)
            {
                return NotFound();
            }
        }

        return View(dto);
    }

    [HttpGet]
    public async Task<IActionResult> Delete(Guid id)
    {
        ContactTask? task = await _contactTaskService.GetByIdAsync(id);
        return task == null ? NotFound() : View(task.ToDto());
    }

    [HttpPost, ActionName("Delete")]
    public async Task<IActionResult> DeleteConfirmed(Guid id)
    {
        OperationResult result = await _contactTaskService.DeleteAsync(id);
        return result.Success
            ? RedirectToEntity(result.RedirectId, result.RedirectType)
            : RedirectToAction("Index", "Contacts");
    }

    [HttpPost]
    public async Task<IActionResult> ToggleComplete(Guid id)
    {
        OperationResult result = await _contactTaskService.ToggleCompleteAsync(id);
        return result.Success
            ? RedirectToEntity(result.RedirectId, result.RedirectType)
            : NotFound();
    }
}
