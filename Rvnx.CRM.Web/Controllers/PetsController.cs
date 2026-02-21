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
        public IActionResult Create(Guid entityId)
        {
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
            if (ModelState.IsValid)
            {
                Pet pet = petDto.ToEntity();
                pet.EntityType = EntityTypes.Person;
                await _repository.AddAsync(pet);
                await _repository.SaveChangesAsync();

                return RedirectToEntity(petDto.EntityId, EntityTypes.Person);
            }
            return View(petDto);
        }

        public async Task<IActionResult> Edit(Guid id)
        {
            Pet? pet = await _repository.GetByIdAsync<Pet>(id);
            if (pet == null) return NotFound();

            PetFormDto dto = new()
            {
                Id = pet.Id,
                EntityId = pet.EntityId,
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
            if (id != petDto.Id) return NotFound();

            if (ModelState.IsValid)
            {
                Pet? pet = await _repository.GetByIdAsync<Pet>(id);
                if (pet == null) return NotFound();

                pet.UpdateEntity(petDto);
                await _repository.UpdateAsync(pet);
                await _repository.SaveChangesAsync();

                return RedirectToEntity(pet.EntityId, pet.EntityType);
            }
            return View(petDto);
        }

        public async Task<IActionResult> Delete(Guid id)
        {
            Pet? pet = await _repository.GetByIdAsync<Pet>(id);
            return pet == null ? NotFound() : View(pet.ToDto());
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            Pet? pet = await _repository.GetByIdAsync<Pet>(id);
            if (pet != null)
            {
                Guid entityId = pet.EntityId;
                await _repository.DeleteAsync<Pet>(id);
                await _repository.SaveChangesAsync();
                return RedirectToEntity(entityId, pet.EntityType);
            }
            return RedirectToAction("Index", "Contacts");
        }
    }
}
