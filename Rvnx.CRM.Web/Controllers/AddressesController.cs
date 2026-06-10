using Microsoft.AspNetCore.Mvc;
using Rvnx.CRM.Core.DTOs.Contact;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models;
using Rvnx.CRM.Web.Controllers.Base;

namespace Rvnx.CRM.Web.Controllers;

public class AddressesController(IAddressService addressService, IRepository repository) : RepositoryController(repository)
{
    private readonly IAddressService _addressService = addressService;

    [HttpGet]
    public async Task<IActionResult> Create(Guid contactId)
    {
        AddressFormDto? dto = await _addressService.GetFormForCreateAsync(contactId);
        return dto == null ? NotFound() : View(dto);
    }

    [HttpPost]
    public async Task<IActionResult> Create(AddressFormDto dto)
    {
        if (ModelState.IsValid)
        {
            IActionResult? handled = HandleOperationResult(await _addressService.CreateAsync(dto));
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
        AddressFormDto? dto = await _addressService.GetFormAsync(id);
        return dto == null ? NotFound() : View(dto);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(Guid id, AddressFormDto dto)
    {
        if (id != dto.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            IActionResult? handled = HandleOperationResult(await _addressService.UpdateAsync(id, dto));
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
        OperationResult result = await _addressService.DeleteAsync(id);
        return result.Success
            ? RedirectToContact(result.RedirectId)
            : RedirectToAction("Index", "Contacts");
    }
}
