using Microsoft.AspNetCore.Mvc;
using Rvnx.CRM.Core.DTOs.Contact;
using Rvnx.CRM.Core.Extensions;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Web.Controllers.Base;

namespace Rvnx.CRM.Web.Controllers;

public class AddressesController(IAddressService addressService, IRepository repository) : RepositoryController(repository)
{
    private readonly IAddressService _addressService = addressService;

    [HttpGet]
    public async Task<IActionResult> Create(Guid entityId)
    {
        AddressFormDto? dto = await _addressService.GetFormForCreateAsync(entityId);
        return dto == null ? NotFound() : View(dto);
    }

    [HttpPost]
    public async Task<IActionResult> Create(AddressFormDto dto)
    {
        if (ModelState.IsValid)
        {
            OperationResult result = await _addressService.CreateAsync(dto);
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
            OperationResult result = await _addressService.UpdateAsync(id, dto);
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
        Address? address = await _addressService.GetByIdAsync(id);
        return address == null ? NotFound() : View(address.ToDto());
    }

    [HttpPost, ActionName("Delete")]
    public async Task<IActionResult> DeleteConfirmed(Guid id)
    {
        OperationResult result = await _addressService.DeleteAsync(id);
        return result.Success
            ? RedirectToEntity(result.RedirectId, result.RedirectType)
            : RedirectToAction("Index", "Contacts");
    }
}
