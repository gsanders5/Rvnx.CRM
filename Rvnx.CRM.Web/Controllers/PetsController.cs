using Microsoft.AspNetCore.Mvc;
using Rvnx.CRM.Core.Constants;
using Rvnx.CRM.Core.DTOs.Contact;
using Rvnx.CRM.Core.Extensions;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Web.Controllers.Base;

namespace Rvnx.CRM.Web.Controllers
{
    public class PetsController(IRepository repository) : RepositoryController(repository)
    {
        public async Task<IActionResult> Create(Guid entityId)
        {
            if (await IsPartialContactAsync(entityId)) return NotFound();
            PetFormDto dto = new()
            {
                EntityId = entityId
            };
            return View(dto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PetFormDto petDto)
        {
            if (await IsPartialContactAsync(petDto.EntityId)) return NotFound();

            if (ModelState.IsValid)
            {
                Pet pet = petDto.ToEntity();
                await Repository.AddAsync(pet);
                await Repository.SaveChangesAsync();

                return RedirectToEntity(petDto.EntityId, EntityTypes.Person);
            }
            return View(petDto);
        }

        public async Task<IActionResult> Edit(Guid id)
        {
            Pet? pet = await Repository.GetByIdAsync<Pet>(id);
            if (pet == null || await IsPartialContactAsync(pet.ContactId))
            {
                return NotFound();
            }

            PetFormDto dto = new()
            {
                Id = pet.Id,
                EntityId = pet.ContactId,
                Name = pet.Name,
                Species = pet.Species,
                Breed = pet.Breed,
                Birthday = pet.Birthday,
                Notes = pet.Notes
            };

            return View(dto);
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
                Pet? pet = await Repository.GetByIdAsync<Pet>(id);
                if (pet == null || await IsPartialContactAsync(pet.ContactId))
                {
                    return NotFound();
                }

                pet.UpdateEntity(petDto);
                await Repository.UpdateAsync(pet);
                await Repository.SaveChangesAsync();

                return RedirectToEntity(pet.ContactId, EntityTypes.Person);
            }
            return View(petDto);
        }

        public async Task<IActionResult> Delete(Guid id)
        {
            Pet? pet = await Repository.GetByIdAsync<Pet>(id);
            return pet == null || await IsPartialContactAsync(pet.ContactId) ? NotFound() : View(pet.ToDto());
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            Pet? pet = await Repository.GetByIdAsync<Pet>(id);
            if (pet != null)
            {
                Guid entityId = pet.ContactId;
                await Repository.DeleteAsync<Pet>(id);
                await Repository.SaveChangesAsync();
                return RedirectToEntity(entityId, EntityTypes.Person);
            }
            return RedirectToAction("Index", "Contacts");
        }
    }
}
