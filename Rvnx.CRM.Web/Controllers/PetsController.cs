using Microsoft.AspNetCore.Mvc;
using Rvnx.CRM.Core.DTOs.Contact;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models;
using Rvnx.CRM.Web.Controllers.Base;

namespace Rvnx.CRM.Web.Controllers;

public class PetsController(IPetService petService, IRepository repository, IContactReadService contactReadService) : RepositoryController(repository)
{
    private readonly IPetService _petService = petService;
    private readonly IContactReadService _contactReadService = contactReadService;

    [HttpGet]
    public async Task<IActionResult> Create(Guid contactId)
    {
        PetFormDto? dto = await _petService.GetFormForCreateAsync(contactId);
        if (dto == null)
        {
            return NotFound();
        }

        await PopulateContactsSelectListExcludeDeceased(_contactReadService, dto.ContactIds);
        return View(dto);
    }

    [HttpPost]
    public async Task<IActionResult> Create(PetFormDto petDto)
    {
        if (ModelState.IsValid)
        {
            IActionResult? handled = HandleOperationResult(await _petService.CreateAsync(petDto));
            if (handled != null)
            {
                return handled;
            }
        }

        await PopulateContactsSelectListExcludeDeceased(_contactReadService, petDto.ContactIds);
        return View(petDto);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id)
    {
        PetFormDto? dto = await _petService.GetFormAsync(id);
        if (dto == null)
        {
            return NotFound();
        }

        await PopulateContactsSelectListExcludeDeceased(_contactReadService, dto.ContactIds);
        return View(dto);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(Guid id, PetFormDto petDto)
    {
        if (id != petDto.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            IActionResult? handled = HandleOperationResult(await _petService.UpdateAsync(id, petDto));
            if (handled != null)
            {
                return handled;
            }
        }

        await PopulateContactsSelectListExcludeDeceased(_contactReadService, petDto.ContactIds);
        return View(petDto);
    }

    [HttpPost, ActionName("Delete")]
    public async Task<IActionResult> DeleteConfirmed(Guid id)
    {
        OperationResult result = await _petService.DeleteAsync(id);
        return result.Success
            ? RedirectToContact(result.RedirectId)
            : RedirectToAction("Index", "Contacts");
    }

}
