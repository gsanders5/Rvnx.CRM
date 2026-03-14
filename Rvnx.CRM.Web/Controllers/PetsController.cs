using Microsoft.AspNetCore.Mvc;
using Rvnx.CRM.Core.DTOs.Contact;
using Rvnx.CRM.Core.Extensions;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Web.Controllers.Base;

namespace Rvnx.CRM.Web.Controllers
{
    public class PetsController(IPetService petService, IRepository repository) : RepositoryController(repository)
    {
        private readonly IPetService _petService = petService;

        [HttpGet]
        public async Task<IActionResult> Create(Guid entityId)
        {
            PetFormDto? dto = await _petService.GetFormForCreateAsync(entityId);
            return dto == null ? NotFound() : View(dto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PetFormDto petDto)
        {
            if (ModelState.IsValid)
            {
                OperationResult result = await _petService.CreateAsync(petDto);
                if (result.Success)
                {
                    return RedirectToEntity(result.RedirectId, result.RedirectType);
                }

                if (result.ErrorMessage == "Contact not found.")
                {
                    return NotFound();
                }
            }

            return View(petDto);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(Guid id)
        {
            PetFormDto? dto = await _petService.GetFormAsync(id);
            return dto == null ? NotFound() : View(dto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, PetFormDto petDto)
        {
            if (id != petDto.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                OperationResult result = await _petService.UpdateAsync(id, petDto);
                if (result.Success)
                {
                    return RedirectToEntity(result.RedirectId, result.RedirectType);
                }

                if (result.ErrorMessage == "Pet not found.")
                {
                    return NotFound();
                }
            }

            return View(petDto);
        }

        [HttpGet]
        public async Task<IActionResult> Delete(Guid id)
        {
            Pet? pet = await _petService.GetByIdAsync(id);
            return pet == null ? NotFound() : View(pet.ToDto());
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            OperationResult result = await _petService.DeleteAsync(id);
            return result.Success
                ? RedirectToEntity(result.RedirectId, result.RedirectType)
                : RedirectToAction("Index", "Contacts");
        }
    }
}