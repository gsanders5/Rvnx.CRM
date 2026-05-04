using Microsoft.AspNetCore.Mvc;
using Rvnx.CRM.Core.DTOs.Contact;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Web.Controllers.Base;

namespace Rvnx.CRM.Web.Controllers;

public class FactsController(IFactService factService, IRepository repository) : RepositoryController(repository)
{
    private readonly IFactService _factService = factService;

    [HttpGet]
    public async Task<IActionResult> Create(Guid contactId)
    {
        FactFormDto? dto = await _factService.GetFormForCreateAsync(contactId);
        return dto == null ? NotFound() : View(dto);
    }

    [HttpPost]
    public async Task<IActionResult> Create(FactFormDto factDto)
    {
        if (ModelState.IsValid)
        {
            IActionResult? handled = HandleOperationResult(await _factService.CreateAsync(factDto));
            if (handled != null)
            {
                return handled;
            }
        }

        return View(factDto);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        FactFormDto? dto = await _factService.GetFormAsync(id.Value);
        return dto == null ? NotFound() : View(dto);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(Guid id, FactFormDto factDto)
    {
        if (id != factDto.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            IActionResult? handled = HandleOperationResult(await _factService.UpdateAsync(id, factDto));
            if (handled != null)
            {
                return handled;
            }
        }

        return View(factDto);
    }

    [HttpGet]
    public async Task<IActionResult> Delete(Guid? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        Fact? fact = await _factService.GetByIdAsync(id.Value);
        return fact == null ? NotFound() : View(fact);
    }

    [HttpPost, ActionName("Delete")]
    public async Task<IActionResult> DeleteConfirmed(Guid id)
    {
        OperationResult result = await _factService.DeleteAsync(id);
        return result.Success
            ? RedirectToContact(result.RedirectId)
            : RedirectToAction("Index", "Home");
    }
}
