using Microsoft.AspNetCore.Mvc;
using Rvnx.CRM.Core.Constants;
using Rvnx.CRM.Core.DTOs.Pet;
using Rvnx.CRM.Core.Extensions;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Web.Controllers.Base;

namespace Rvnx.CRM.Web.Controllers
{
    public class PetsController : AuthorizedController
    {
        private readonly IRepository _repository;

        public PetsController(IRepository repository)
        {
            _repository = repository;
        }

        // GET: Pets/Create?entityId=...
        public IActionResult Create(Guid entityId)
        {
            var dto = new CreatePetDto
            {
                EntityId = entityId
            };
            return View(dto);
        }

        // POST: Pets/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreatePetDto petDto)
        {
            if (ModelState.IsValid)
            {
                var pet = petDto.ToEntity();
                pet.EntityType = EntityTypes.Person;
                await _repository.AddAsync(pet);
                await _repository.SaveChangesAsync();

                return RedirectToAction("Details", "Contacts", new { id = petDto.EntityId });
            }
            return View(petDto);
        }

        // GET: Pets/Edit/5
        public async Task<IActionResult> Edit(Guid id)
        {
            var pet = await _repository.GetByIdAsync<Pet>(id);
            if (pet == null) return NotFound();

            var dto = new UpdatePetDto
            {
                Id = pet.Id,
                Name = pet.Name,
                Species = pet.Species,
                Breed = pet.Breed,
                Birthday = pet.Birthday,
                Notes = pet.Notes
            };

            ViewBag.EntityId = pet.EntityId;

            return View(dto);
        }

        // POST: Pets/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, UpdatePetDto petDto)
        {
            if (id != petDto.Id) return NotFound();

            if (ModelState.IsValid)
            {
                var pet = await _repository.GetByIdAsync<Pet>(id);
                if (pet == null) return NotFound();

                pet.UpdateEntity(petDto);
                await _repository.UpdateAsync(pet);
                await _repository.SaveChangesAsync();

                return RedirectToAction("Details", "Contacts", new { id = pet.EntityId });
            }
            return View(petDto);
        }

        // GET: Pets/Delete/5
        public async Task<IActionResult> Delete(Guid id)
        {
            var pet = await _repository.GetByIdAsync<Pet>(id);
            return pet == null ? NotFound() : View(pet.ToDto());
        }

        // POST: Pets/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var pet = await _repository.GetByIdAsync<Pet>(id);
            if (pet != null)
            {
                var entityId = pet.EntityId;
                await _repository.DeleteAsync<Pet>(id);
                await _repository.SaveChangesAsync();
                return RedirectToAction("Details", "Contacts", new { id = entityId });
            }
            return RedirectToAction("Index", "Contacts");
        }
    }
}
